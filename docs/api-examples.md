# API examples

The examples below assume the API is available at `http://localhost:5260`.

## Search for a card

```bash
curl "http://localhost:5260/api/Cards/Lightning%20Bolt"
```

## Ask the advisor

`/api/Advisor/ask` currently accepts a JSON string as the request body.

```bash
curl -X POST "http://localhost:5260/api/Advisor/ask" \
  -H "Content-Type: application/json" \
  -d '"How does this card interaction work?"'
```

## Inspect retrieval context

```bash
curl "http://localhost:5260/api/RagAdmin/test-retrieval?question=How%20does%20graveyard%20interaction%20work%3F"
```

## Build the vector knowledge base

```bash
curl -X POST "http://localhost:5260/api/RagAdmin/build-knowledge-base"
```

Then inspect progress:

```bash
curl "http://localhost:5260/api/RagAdmin/status"
```

## Analyze a matchup screenshot

```bash
curl -X POST "http://localhost:5260/api/Advisor/analyze-match" \
  -F "BoardScreenshot=@board.png" \
  -F "MainDeckList=4 Card A\n4 Card B" \
  -F "SideboardList=3 Card C\n2 Card D"
```
