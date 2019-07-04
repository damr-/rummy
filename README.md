# Romme/Rummy
A Unity application which plays the famous game 'Rummy' against itself. I created this to find out whether it would be advantageous to set the minimum value for laying down cards to 30 compared to the 'usual' 40.
The application was used to simulate many hundreds of games to create meaningful statistics. The downloadable versions are not meant for simulation and data gathering, but for presenting the application. Therefore, these versions do not track stats and include an option-menu which lets the user control some of the most important values of the simulation.

[Download (Windows)](https://github.com/damr-/romme/raw/master/Builds/Romme_Windows.zip)

[Download (Mac OS)](https://github.com/damr-/romme/raw/master/Builds/Romme_MacOS.app.zip)

Obviously, it would have been better to simulate these games without the visual representation, allowing for much higher computation speed. The visuals were initially implemented to test the system for errors, since the whole logic behind the player behavior was created from scratch. The setup was then kept in the process and also improved upon with various features. For example, all possible card packs and their values, possible single cards and duos which were detected are logged on the screen for an easier understanding of the player's decision making. This information turned out to be very useful when watching the simulation, therefore it stayed.

## Rules
- 2 players, each start with 13 cards in their hand.
- At the beginning of their turn, the current player draws a card.
- To end the turn, the player discards one card.
- After round 2, if a player can form card packs with a combined value greater than a minimum required value, these packs are laid down. Card packs are sets and runs.
  - Sets are formed by cards of the same rank but different suits. A set consists of 3 or 4 cards, one of which can be replaced by a joker of the appropriate color.
  - Runs consist of at least 3 cards of the same suit which form an ascending sequence. A run can start with Ace-2-3 and end with Q-K-Ace but must not form loops (K-Ace-2 segments are forbidden). One card in a run can be replaced by a joker of the matching color (adding more joker cards later is not forbidden though).
- A player who has laid down cards possesses the ability to
  - pick up the top card of the discard stack instead of drawing one from the card stack at the start of their turn,
  - lay down sets/runs of any value during their turn,
  - add single cards to already laid down card packs during their turn. A card can also be added to a card pack where the joker originally replaced the card in question. In that case, the card replaces the joker and the latter is picked up by the player who replaced it.
- The goal is to get rid of all the cards in one's hand. This means the all the cards in the player's hand are gone, except one, which is then discarded to finish the game.

## Remarks
- The players always try to lay down the highest valued card combination. They will never purposefully keep a finished card pack on their hands (unless they cannot yet lie down at all). Same goes for single laydown cards.
- The players do not "count"/memorize the cards which had been discarded to figure out possible card combinations.
- Players *do* check for sets/runs in their hand cards which only consist of two cards ("duos") and try to keep them on hand for as long as possible. Since every card exists twice in the game, if the duo's missing third card is part of already laid down card packs twice, the duo will *not* be kept, since it cannot be completed.
- Starting with 13 cards, there exists the possibility that the game ends in a draw. This happens when both players laid down 3 complete sets of 4 cards each. With one card remaining in their hand, no player can win. In that case, the game is forcefully ended as a draw.
- The seed is used to shuffle the cards in the deck. As a result, running the application with the same seed always results in the same game being played.
- LINQ is used for the logic behind finding possible sets/runs and duos. That code could definitely be improved a lot both in terms of elegance, shortness and performance.
