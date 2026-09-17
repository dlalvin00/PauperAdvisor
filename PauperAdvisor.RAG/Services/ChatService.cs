using OllamaSharp;
using PauperAdvisor.Domain.Decks;
using PauperAdvisor.RAG.Configuration;
using System.Text;

namespace PauperAdvisor.RAG.Services;

public class ChatService
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly RetrievalService _retrievalService;
    public ChatService(RetrievalService retrievalService, OllamaOptions options)
    {
        _retrievalService = retrievalService;
        _ollamaClient = new OllamaApiClient(new Uri(options.BaseUrl))
        {
            SelectedModel = options.ChatModel
        };
    }

    public async Task<string> AskAdvisorAsync(string question)
    {
        var context = await _retrievalService.RetrieveContextAsync(question, limit: 15);

        var systemPrompt = $@"You are an expert Magic: The Gathering advisor specializing in the Pauper format.
            Your goal is to answer the user's question accurately.
            
            CRITICAL RULES:
            1. You MUST base your answer EXCLUSIVELY on the 'KNOWLEDGE BASE CONTEXT' provided below.
            2. If the answer cannot be found in the context, explicitly state: 'I do not have enough information in my database to answer this.'
            3. DO NOT hallucinate card names, mechanics, or rulings.
            
            {context}";

        // Utiliza o objeto Chat moderno do OllamaSharp passando o cliente e o system prompt
        var chat = new Chat(_ollamaClient, systemPrompt);

        var responseBuilder = new StringBuilder();

        // Envia a mensagem e consome o stream de tokens de forma assíncrona
        await foreach (var answerToken in chat.SendAsync(question))
        {
            responseBuilder.Append(answerToken);
        }

        return responseBuilder.ToString();
    }

    public async Task<string> AnalyzeMatchupAsync(Stream imageStream, DeckList mainDeck, DeckList sideboard, string question)
    {
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        // --- PASSO 1: EXTRAÇÃO VISUAL (OCR IA) ---
        var visionPrompt = "Extract ONLY the names of the Magic: The Gathering cards visible on the battlefield and in the graveyard. Return strictly a comma-separated list of card names. Do not invent names. Do not include any other text or explanation.";

        var visionChat = new Chat(_ollamaClient, visionPrompt);
        var rawOcrBuilder = new StringBuilder();

        await foreach (var token in visionChat.SendAsync("Read the cards in this image.", new List<byte[]> { imageBytes }))
        {
            rawOcrBuilder.Append(token);
        }

        // --- PASSO 2: VALIDAÇÃO NO SQLITE ---
        var validatedOpponentCards = await _retrievalService.ValidateCardNamesAsync(rawOcrBuilder.ToString());
        var verifiedOpponentBoard = string.Join(", ", validatedOpponentCards);

        if (string.IsNullOrWhiteSpace(verifiedOpponentBoard))
            return "Não consegui identificar nenhuma carta válida na imagem. Tente um print com mais resolução.";

        // --- PASSO 3: ANÁLISE ESTRATÉGICA E SIDEBOARD ---
        var strategyPrompt = $@"You are an expert MTG Pauper sideboarding algorithm.
            Based on the opponent's confirmed cards, identify their archetype and calculate the optimal sideboard swap.
            
            OPPONENT'S CONFIRMED CARDS: {verifiedOpponentBoard}
            MY MAIN DECK: {FormatDeckList(mainDeck)}
            MY SIDEBOARD: {FormatDeckList(sideboard)}
            
            CRITICAL SYSTEM RULES:
            1. NEVER suggest removing Lands (e.g., Mountains, Swamps, Bridges, Gorges).
            2. MATHEMATICAL MATCH: The total number of cards in the 'In' array MUST EXACTLY MATCH the total number of cards in the 'Out' array.
            3. IN ONLY: You can only suggest cards that exist in MY SIDEBOARD.
            4. OUT ONLY: You can only suggest cards that exist in MY MAIN DECK.
            5. NO HALLUCINATIONS: Nihil Spellbomb targets graveyards, not creatures or lands. Electrickery deals 1 damage to creatures.
            
            You MUST respond ONLY with a valid JSON object matching this exact structure. Do not include any markdown formatting, greetings, or additional text outside the JSON:
            
            {{
              ""archetype"": ""Identified opponent deck"",
              ""tactics"": ""Brief explanation of the matchup strategy"",
              ""in"": [""3 Nihil Spellbomb"", ""1 Electrickery""],
              ""out"": [""4 Grab the Prize""]
            }}";

        // Uso um novo chat sem a imagem para focar apenas na lógica textual
        var strategyChat = new Chat(_ollamaClient, strategyPrompt);
        var strategyBuilder = new StringBuilder();

        await foreach (var token in strategyChat.SendAsync(question))
        {
            strategyBuilder.Append(token);
        }

        return strategyBuilder.ToString();
    }

    private static string FormatDeckList(
    DeckList deckList)
    {
        return string.Join(
            Environment.NewLine,
            deckList.Cards.Select(
                card =>
                    $"{card.Quantity} {card.Name}"));
    }
}