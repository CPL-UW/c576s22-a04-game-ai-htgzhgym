What I did:
1. implement the game-ai using BFS for seaching the best score that can be reached in 5 moves
2. disabled the angrypoint feature. Implement a feature that will add lines to enemy when linekill
3. add a leap method which may return the leap(height difference between each column)
3. when enemy has a leap > 5, force the next piece to be I-piece and try fix the leap

score evaluation rubric: (importance sorted)
add score:
1. when get a linekill 
2. fix leap (only I-piece and only the last column)

lose score:
1. has blank spot after drop the piece.
2. not fixing leap (only I-piece and only the last column)
3. average height
4. average leap



----------------------------
# CS576-Tetris-Sample

1. Clone this repo
2. Open it in a 2021 version of Unity
3. 2xClick TetrisScene (in Assets/Scenes)
4. Hit run?

Apologies for the mess. YMMV.
