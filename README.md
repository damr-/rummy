# Romme
A Unity application which plays the famous game 'Romme' against itself. I created this to find out whether it would be advantageous in any way to set the minimum value for laying down cards to 30 compared to the 'usual' 40.

## Rules
- 2 Players, each start with 13 cards in their hands
- The goal is to get rid of all the cards in one's hand
- At the beginning of each turn, the current player draws a card
- To end the turn, the player discards one card
- After round 2, if a player can form card packs with a combined value greater than a minimum required value, these packs are laid down. Card packs are sets and runs
  - Sets are formed by cards of the same rank but different suits. A set consists of 3 or 4 cards, one of which can be replaced by a joker of the appropriate color.
  - Runs consist of at least 3 cards of the same suit which form an ascending order. A run can start with Ace-2-3 and end with Q-K-Ace but must not form loops (K-Ace-2 segments are forbidden). One card in a run can be replaced by a joker of the matching color (adding more joker cards later is not forbidden though).
- A player who has laid down cards possesses the ability to
  - pick up the top card of the discard stack instead of drawing one from the card stack at the start of their turn,
  - lay down sets/runs of any value during their turn,
  - adding single cards to already laid down card packs
