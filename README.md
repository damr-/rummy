# Rummy
A Unity application which plays the famous game 'Rummy' against itself.

[Run in Browser (WebGL)](https://damr-.github.io/rummy/Builds/rummy/)

## Specific Rules
- 2-6 players (1 human or AI, others AI), each starts with 13 cards in their hand.
- Card packs (melds) can be laid if their summed value is greater than 40.
  - Sets are formed by 3 or 4 cards of the same rank but different suits. In a set of 3 cards, one can be replaced by a joker of the same color. Adding a joker with the correct color to a set of 3 cards is allowed.
  - Runs consist of at least 3 cards of the same suit which form an ascending sequence. Aces can be used for A-2-3 and Q-K-A but not K-A-2. When creating a run of 3 cards, only one card can be replaced by a joker of the same color (adding more joker cards later is allowed).
- A player who has laid down melds can
  - pick up the top card of the discard stack instead of drawing one from the card stack at the start of their turn,
  - lay down sets/runs of any value,
  - add single cards to already laid down melds,
  - swap out Jokers in melds with the appropriate card.

## Remarks about AI players
- They always try to lay down the highest valued card combination. They will never purposefully keep a finished meld on their hands. Same goes for single cards.
- They will always lay down any possible single card to the first possible spot they find (thus possibly missing a different, more effective, combination).
- They do not count/memorize the cards which had been discarded to figure out possible card combinations.
- Once an AI player has laid down the initial melds they always try to get rid of the highest valued card to minimize losses if they lose the game.
- They *do* check for sets/runs in their hand cards which only consist of two cards ("duos") and try to keep them on hand for as long as possible. Since every card exists twice in the game, if the duo's missing card has already been laid down twice, the duo will *not* be kept.
- Starting with 13 cards, it is possible that the game ends in a draw. This happens when both players laid down 3 complete sets of 4 cards each. With one card remaining in their hand, neither player can win. In that case, the game is forcefully ended as a draw.
