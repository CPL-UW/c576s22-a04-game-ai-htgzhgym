using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

// ReSharper disable once InconsistentNaming
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class GMScript : MonoBehaviour
{
    public TileBase pieceTile;
    public TileBase emptyTile;
    public TileBase chunkTile;
    //public TileBase[] numberTiles;
    public Tilemap boardMap;
    public Tilemap enemyMap;
    public TMP_Text infoText;
    private int _score;
    private int _difficulty;
    private int _fixedUpdateFramesToWait = 4;
    private int _fixedUpdateCount;

    // ReSharper disable once InconsistentNaming
    public bool DEBUG_MODE;
    private bool Dirty { get; set; }
    private bool _initialized;
    private const int BOUNDS_MAX = 25;
    private int _minBx = BOUNDS_MAX;
    private int _minBy = BOUNDS_MAX;
    private int _maxBx = -BOUNDS_MAX;
    private int _maxBy = -BOUNDS_MAX;
    private int _minEx = BOUNDS_MAX;
    private int _minEy = BOUNDS_MAX;
    private int _maxEx = -BOUNDS_MAX;
    private int _maxEy = -BOUNDS_MAX;

    private int _inARow;

    // private int _width = 0, _height = 0;

    private Vector3Int[] _myPiece;
    private Vector3Int[] _myChunk;
    private Vector3Int[] _enemyPiece;
    private Vector3Int[] _enemyChunk;
    
    private Vector3Int[] PIECE_T;
    private Vector3Int[] PIECE_L;
    private Vector3Int[] PIECE_Z;
    private Vector3Int[] PIECE_J;
    private Vector3Int[] PIECE_S;
    private Vector3Int[] PIECE_I;
    private Vector3Int[][] PIECES;

    private void InitializePieces()
    {
        PIECE_T = new Vector3Int[] { new(0,-1), new(1,-1), new(0,0),  new(-1,-1) };
        PIECE_L = new Vector3Int[] { new(0,-1), new(1,-1), new(1,0),  new(-1,-1) };
        PIECE_J = new Vector3Int[] { new(0,-1), new(1,-1), new(-1,0), new(-1,-1) };
        PIECE_S = new Vector3Int[] { new(0,-1), new(-1,-1),new(0,0),  new(1,0) };
        PIECE_Z = new Vector3Int[] { new(0,-1), new(1,-1), new(0,0),  new(-1,0) };
        PIECE_I = new Vector3Int[] { new(0,0),  new(-1,0), new(-2,0), new(1,0) };
        PIECES = new []{PIECE_T,PIECE_L,PIECE_Z,PIECE_J,PIECE_S,PIECE_I};
    }
    
    
    
    private Vector3Int[] CreateAPiece(int midX, int maxY)
    {
        var targetPiece = PIECES[Random.Range(0, 6)];
        var newPiece = new Vector3Int[targetPiece.Length];
        for (var i = 0; i < targetPiece.Length; i++)
        {
            newPiece[i].x = targetPiece[i].x + midX;
            newPiece[i].y = targetPiece[i].y + maxY;
        }
        return newPiece;

    }private Vector3Int[] CreateAnIPiece(int midX, int maxY)
    {
        var targetPiece = PIECE_I;
        var newPiece = new Vector3Int[targetPiece.Length];
        for (var i = 0; i < targetPiece.Length; i++)
        {
            newPiece[i].x = targetPiece[i].x + midX;
            newPiece[i].y = targetPiece[i].y + maxY;
        }
        return newPiece;
    }



    private void BlankABoard(Tilemap map,int x1, int y1, int x2, int y2)
    {
        for (var j = y1; j <= y2; j++)
        for (var i = x1; i <= x2; i++)
        {
            map.SetTile(new Vector3Int(i,j,0),emptyTile);
        }
    }

    private void BlankAllBoards()
    {
        BlankABoard(boardMap,_minBx,_minBy,_maxBx,_maxBy);
        BlankABoard(enemyMap,_minEx,_minEy,_maxEx,_maxEy);
    }

    private void SetupBaseBoards()
    {
        // Find the bounds for the visible board
        _initialized = true;
        for (var wy = -1 * BOUNDS_MAX; wy < BOUNDS_MAX; wy++)
        for (var wx = -1 * BOUNDS_MAX; wx < BOUNDS_MAX; wx++)
        {
            var myTile = boardMap.GetTile(new Vector3Int(wx,wy,0));
            var enemyTile = enemyMap.GetTile(new Vector3Int(wx,wy,0));
            if (myTile)
            {
                if (wx < _minBx) _minBx = wx;
                if (wy < _minBy) _minBy = wy;
                if (wx > _maxBx) _maxBx = wx;
                if (wy > _maxBy) _maxBy = wy;
            }
            if (enemyTile)
            {
                if (wx < _minEx) _minEx = wx;
                if (wy < _minEy) _minEy = wy;
                if (wx > _maxEx) _maxEx = wx;
                if (wy > _maxEy) _maxEy = wy;
            }
        }

        BlankAllBoards();
        _myPiece = CreateAPiece((_minBx + _maxBx)/2,_maxBy);
        _enemyPiece = CreateAPiece((_minEx + _maxEx) / 2, _maxEy);
        if (!ValidPiece(_myPiece, true))
        {
            Debug.Log("NO VALID MOVES FROM START");
            Debug.Break();
        }

        Debug.Log($"MY BOARD SIZE = {(1 + _maxBx - _minBx)} x {(1 + _maxBy - _minBy)} ({_minBx},{_minBy}) -> ({_maxBx},{_maxBy})");
        Debug.Log($"AI BOARD SIZE = {(1 + _maxEx - _minEx)} x {(1 + _maxEy - _minEy)} ({_minEx},{_minEy}) -> ({_maxEx},{_maxEy})");
    }

    private static Vector3Int[] KillRow(Vector3Int[] chunk, int row)
    {
        var newChunk = new Vector3Int[] { };
        foreach (var p in chunk)
        {
            if (p.y > row)
            {
                Vector3Int [] movedPieces = {new(p.x, p.y - 1, p.z)};
                newChunk = newChunk.Concat(movedPieces).ToArray();
            } else if (p.y < row)
            {
                Vector3Int [] movedPieces = {p};
                newChunk = newChunk.Concat(movedPieces).ToArray();
            }
        }
        return newChunk;
    }

    private const int NO_ROW = -10 * BOUNDS_MAX; 
    private static int FindKillableRow(Vector3Int[] chunk, int max_width)
    {
        if (null == chunk) return NO_ROW;
        for (var row = -BOUNDS_MAX; row <= BOUNDS_MAX; row++) // just MIN_BOUND to MAX_BOUND?
        {
            var maxCount = max_width;//_maxBx - _minBx + 1; // width
            foreach (var p in chunk)
            {
                if (p.y == row)
                {
                    maxCount--;
                }
            }

            if (0 == maxCount)
            {
                return row;
            }
        }

        return NO_ROW;   
    }
    
    private bool ValidWorldXY(int wx, int wy, bool player)
    {
        if (player)
            return (wx <= _maxBx && wx >= _minBx && wy <= _maxBy && wy >= _minBy);
        return (wx <= _maxEx && wx >= _minEx && wy <= _maxEy && wy >= _minEy);
    }

    private bool ValidMoveXY(int wx, int wy, bool player)
    {
        if (!ValidWorldXY(wx, wy, player))
            return false;
        if (player)
            return null == _myChunk || _myChunk.All(p => p.x != wx || p.y != wy);
        return null == _enemyChunk || _enemyChunk.All(p => p.x != wx || p.y != wy);
    }

    private bool ValidPiece(Vector3Int[] piece,bool player)
    {
        return null != piece && piece.All(p => ValidMoveXY(p.x, p.y, player));
    }

    private Vector3Int[] ShiftPiece(IReadOnlyList<Vector3Int> piece, int dx, int dy, bool player)
    {
        if (null == piece) return null;
        var outPiece = new Vector3Int[piece.Count];
        foreach (var p in piece)
        {
            if (!ValidMoveXY(p.x + dx, p.y + dy, player))
            {
                // if (DEBUG_MODE) Debug.Log($"INVALID MOVE = {p.x + dx}, {p.y + dy}");
                return null;
            }
        }
        for (var i = 0; i < piece.Count; i++)
        {
            outPiece[i] = new Vector3Int(piece[i].x + dx, piece[i].y + dy);
        }
        
        return outPiece;
    }

    private Vector3Int[] RotatePiece(Vector3Int[] piece, bool player)
    {
        // rotated_x = (current_y + origin_x - origin_y)
        // rotated_y = (origin_x + origin_y - current_x - ?max_length_in_any_direction)
        if (null == piece) return null;
        var newPiece = new Vector3Int[piece.Length];
        Array.Copy(piece,newPiece,piece.Length);

        var origin = piece[0];
        for (var i = 1; i < piece.Length; i++ )
        {
            var rotatedX = piece[i].y + origin.x - origin.y;
            var rotatedY = origin.x + origin.y - piece[i].x;
            if (!ValidMoveXY(rotatedX, rotatedY, player))
                return piece;
            newPiece[i] = new Vector3Int(rotatedX, rotatedY);
        }

        //Array.Copy(newPiece, piece, piece.Length);
        return newPiece;
    }

    private static Vector3Int RandomEnemyPointInRange(int x1, int y1, int x2, int y2)
    {
        return new Vector3Int(Random.Range(x1,x2),Random.Range(y1,(y1 + y2)/2));
    }

    private Vector3Int[] AddChunkAtPoint(Vector3Int[] chunk, Vector3Int chunkPoint)
    {
        chunk ??= new Vector3Int[] {};
        if (chunk.Any(p => p.x == chunkPoint.x && p.y == chunkPoint.y))
            return chunk;
        return chunk.Concat(new [] {chunkPoint}).ToArray();
    }
    
    private Vector3Int[] MoveUpChunks(Vector3Int[] chunk,int row)
    {
        chunk ??= new Vector3Int[] {};
        var newChunk = new Vector3Int[] { };
        foreach (var p in chunk)
        {
          
             Vector3Int[] movedPieces = { new(p.x, p.y + row, p.z) };
             newChunk = newChunk.Concat(movedPieces).ToArray();
            
        }
        return newChunk;
    }

    private Vector3Int[] DropPiece(Vector3Int[] piece, bool player)
    {
        var lastPiece = piece;
        while (null != lastPiece)
        {
            piece = lastPiece;
            lastPiece = ShiftPiece(piece,0, -1, player);
        }
        return piece;
    }

    private static Vector3Int[] ChunkPiece(Vector3Int[] piece, Vector3Int[] chunk)
    {
        chunk ??= new Vector3Int[] {};
        if (null == piece) return chunk;
        return chunk.Concat(piece).ToArray();
    }
    
    private void PlayerDoLeft()
    {
        Dirty = true;
        var tmpPiece = ShiftPiece(_myPiece, -1,0, true);
        if (null != tmpPiece)
            _myPiece = tmpPiece;
    }

    private void PlayerDoRight()
    {
        Dirty = true;
        var tmpPiece = ShiftPiece(_myPiece, 1,0, true);
        if (null != tmpPiece)
            _myPiece = tmpPiece;
    }

    private void PlayerDoUp()
    {
        Dirty = true;
        _myPiece = RotatePiece(_myPiece, true);
    }

    private void PlayerDoDown()
    {
        Dirty = true;
        var tmpPiece = ShiftPiece(_myPiece, 0, -1, true);
        if (null == tmpPiece)
        {
            _myChunk = ChunkPiece(_myPiece, _myChunk);
            _myPiece = null;
        }
        else
        {
            _myPiece = tmpPiece;
        }
    }

    // private string ChunkToString(Vector3Int[] chunk)
    // {
    //     var output = "";
    //     var min_y = chunk.Min(p => p.y);
    //     var min_x = chunk.Min(p => p.x);
    //     var max_y = chunk.Max(p => p.y);
    //     var max_x = chunk.Max(p => p.x);
    //     var truth_board = new bool[max_x-min_x+2,max_y-min_y+2];
    //     foreach (var p in chunk)
    //     {
    //         truth_board[p.x - min_x, p.y - min_y] = true;
    //     }
    //     for (var x = min_x; x <= max_x; x++)
    //     {
    //         for (var y = min_y; y <= max_y; y++)
    //         {
    //             output += truth_board[x - min_x, y - min_y] ? "." : " ";
    //         }
    //         output += "\n";
    //     }
    //
    //     return output;
    // } 
    private const int GOOD_SCORE = 10000;
    private int EvaluateEnemyPieceScore(Vector3Int[] piece, Vector3Int[] chunk, bool drop = true)
    {
        int result = 1000;
        if (null == piece || null == chunk) return -GOOD_SCORE;
        var dp = DropPiece(piece, false);
        int[] dropX = { dp[0].x, dp[1].x, dp[2].x, dp[3].x };
        int[] dropY = { dp[0].y, dp[1].y, dp[2].y, dp[3].y, };
        
        var combined = drop ? DropPiece(piece,false).Concat(chunk).ToArray() : piece.Concat(chunk).ToArray();
        for(int i = 0; i < dropX.Count(); i++)
        {
            if (dropY[i] - 1 >= -10
                && !combined.Contains(new Vector3Int(dropX[i], dropY[i] - 1)))
            {
                result -= 100;
                //Debug.Log("deduction");
            }
            if (dropY[i] - 2 >= -10
                && !combined.Contains(new Vector3Int(dropX[i], dropY[i] - 2)))
            {
                result -= 50;
                //Debug.Log("deduction");
            }
            var segment = new ArraySegment<int>(HeightLeap(combined),0, 6);
            if (segment.Max() > 3)
            {
                result -= 100;
            }
            if (dp.Max(p => p.x) == _maxEx && dropY[0]==dropY[1] && dropY[1] == dropY[2] && dropY[2] == dropY[3])
            {
                result -= 50;
            }
            if (dp.Max(p => p.x) == _maxEx && dropX[0] == dropX[1] && dropX[1] == dropX[2] && dropX[2] == dropX[3])
            {
                result += 50;
            }
        }
        var row = FindKillableRow(combined, _maxEx - _minEx + 1);
        if (row != NO_ROW)
        {
            Debug.Log("FOUND A LINE: ");
            return GOOD_SCORE; // LINE!
        }
        
        result -= (int)(40 * combined.Average(p => p.y +10) + 10 * HeightLeap(combined).Average());
        Debug.Log("result: "+result);
        //Debug.Log((int)(100 * (10 + (-10 * HeightLeap(combined).Average()))));
        return result;
    }


    // strategy: leave maxEy blank until I-piece come and make a tetris. if >5 draw an I-piece for that. Score = killrow*10000
    // No killrow for any other situation.
    // check the height difference for any two column, if >5 draw an I-piece for that. Score = 50000
    // othertime, draw random piece.

    private List<Vector3Int[]> EnemyChooseAction(Vector3Int[] piece,int round)
    {
        if (piece == null) return new List<Vector3Int[]>();
       
        Queue<List<Vector3Int[]>> q = new();
        List<Vector3Int[]> l = new();
        l.Add(piece);
        q.Enqueue(l);
        List<List<Vector3Int[]>> res = new();
        List<int> resScore= new();
        while (q.Any())
        {
            //Debug.Log("inBFS: "+n);
            
            List<Vector3Int[]> list = q.Dequeue();
            Vector3Int[] tmp = list.Last();
            var enemyGoLeft = ShiftPiece(tmp, -1, 0, false);
            var enemyGoRight = ShiftPiece(tmp, 1, 0, false);
            var enemyGoRotate = RotatePiece(tmp, false);
            Vector3Int[][] enemyOptions = { enemyGoLeft, enemyGoRight, enemyGoRotate, tmp};
            var validOptions = enemyOptions.Where(p => ValidPiece(p, false)).ToArray();
            if (EvaluateEnemyPieceScore(tmp,_enemyChunk) == GOOD_SCORE)
            {
                return list;
            }
            else if(list.Count()==round)
            {
                
                res.Add(list);
                resScore.Add(EvaluateEnemyPieceScore(list.Last(), _enemyChunk));
                
            }else if (list.Count() > round)
            {
                break;
            }
            else
            {
                foreach (Vector3Int[] p in validOptions)
                {
                    //Debug.Log("ineach: ");
                    List<Vector3Int[]> newList = new List<Vector3Int[]>(list);
                    newList.Add(p);
                    q.Enqueue(newList);
                }
            }
           
        }
        int maxScore = -1000;
        int maxIndex = 0;
        for(int i = 0; i < resScore.Count(); i++)
        {
            if (maxScore < resScore.ElementAt(i))
            {
                maxIndex = i;
                maxScore = resScore.ElementAt(i);
            }
        }
       // Debug.Log(String.Join(",",resScore.ToArray()));
        //Debug.Log(res.ElementAt(maxIndex).Count());
        
        return res.ElementAt(maxIndex);
       
    }

    

    

    // return the Leap between each col.  
    private int[] HeightLeap(Vector3Int[] chunk)
    {
        int[] heightEachCol = new int[_maxEx - _minEx +1];
        for(int h = 0; h < heightEachCol.Length; h++)
        {
            heightEachCol[h] = _minEy;
        }   
        int minx = _minEx;
        foreach (var p in chunk)
        {
            if (p.y > heightEachCol[p.x-minx]) heightEachCol[p.x-minx] = p.y;
        }
        int[] leap = new int[heightEachCol.Length-1];
        for (int i = 0; i < heightEachCol.Length - 1; i++)
        {
            leap[i] = Math.Abs(heightEachCol[i + 1] - heightEachCol[i]);
        }

        //Debug.Log("leap: "+String.Join(",",leap));
        return leap;
    }

    List<Vector3Int[]> _chosenAction = new();
    private void EnemyDoAction()
    {
        Dirty = true;
      
        if (null == _enemyPiece)

        {
            int[] leap = HeightLeap(_enemyChunk);
            if (leap.Max() > 5) _enemyPiece = CreateAnIPiece((_minEx + _maxEx) / 2+2, _maxEy-2);
            else _enemyPiece = CreateAPiece((_minEx + _maxEx) / 2, _maxEy-1);

            _chosenAction = EnemyChooseAction(_enemyPiece,5);

            _actionIndex = 0;
            if (!ValidPiece(_enemyPiece, false))
            {
                Debug.Break();
                if (DEBUG_MODE) Debug.Log("ENEMY DEAD");
            }
        }
        else 
        {
          //  Debug.Log(_chosenAction.Count());

            var tmpPiece = _enemyChunk;
            if (!_chosenAction.Any()||_chosenAction.Count()<=_actionIndex)
            {
               tmpPiece = ShiftPiece(_enemyPiece, 0, -1, false);

            } 
            else
            {
                
                tmpPiece = _chosenAction.ElementAt(_actionIndex);
                _actionIndex++;
                Debug.Log(_actionIndex);
                tmpPiece = ShiftPiece(tmpPiece, 0, -_actionIndex, false);
                
            }
            if (!ValidPiece(tmpPiece, false))
            {
                _enemyChunk = ChunkPiece(_enemyPiece, _enemyChunk);
                _enemyPiece = null;
            }
            else
            {
                _enemyPiece = tmpPiece;
                //_enemyPiece = EnemyChooseAction(tmpPiece);
                //_enemyPiece = EnemyChooseRecursive(tmpPiece);
            }
        }
    }
    private void PlayerDoDrop()
    {
        Dirty = true;
        _myPiece = DropPiece(_myPiece, true);
    }

    private void DrawAllBoards()
    {
        BlankAllBoards();

        if (null != _myChunk)
        {
            foreach (var p in _myChunk)
            {
                boardMap.SetTile(p, chunkTile);
            }
        }

        if (null != _enemyChunk)
        {
            foreach (var p in _enemyChunk)
            {
                enemyMap.SetTile(p, chunkTile);
            }
        }
        
    }

    private void DrawAllPieces()
    {
        if (null != _myPiece)
        {
            foreach (var p in _myPiece)
            {
                boardMap.SetTile(p, pieceTile);
            }
        }

        if (null != _enemyPiece)
        {
            foreach (var p in _enemyPiece)
            {
                enemyMap.SetTile(p, pieceTile);
            }
        }
    }

    private void MakeRandomAngryChunk()
    {
        _myChunk = AddChunkAtPoint(_myChunk,RandomEnemyPointInRange(_minBx,_minBy,_maxBx,_maxBy));
    }

    private void AddRowsForEnemy(int row)
    {
        _enemyChunk = MoveUpChunks(_enemyChunk, row);
        for (int i = _minEy; i <= _minEy+row; i++)
        {
            int randj = _maxEx;
            for(int j = _minEx; j <= _maxEx; j++)
            {
                if (j == randj) continue;
                _enemyChunk = AddChunkAtPoint(_enemyChunk, new(j,i));
            }
        }
    }
    private void AddRowsForPlayer(int row)
    {
        _myChunk = MoveUpChunks(_myChunk, row);
        for (int i = _minBy; i <= _minBy+row; i++)
        {
            int randj = _minBx;
            for(int j = _minBx; j <= _maxBx; j++)
            {
                if (j == randj) continue;
                _myChunk = AddChunkAtPoint(_myChunk, new(j,i));
            }
        }
    }


    int _actionIndex = 0;
    void FixedUpdate()
    {
        if (0 != _fixedUpdateCount++ % _fixedUpdateFramesToWait) return;
        PlayerDoDown();
        EnemyDoAction();
        if (_inARow > _difficulty)
        {
            _difficulty = _inARow;
            if (_fixedUpdateFramesToWait > 1)
            {
                _fixedUpdateFramesToWait--;
            }
        }

        var row_to_kill = FindKillableRow(_myChunk,_maxBx - _minBx + 1);
        if (NO_ROW != row_to_kill)
        {
            _myChunk = KillRow(_myChunk, row_to_kill);
            
            AddRowsForEnemy(1);
            //MakeRandomAngryChunk();
        }
        else
        {
            _inARow = 0;
        }
        var enemy_row_to_kill = FindKillableRow(_enemyChunk, _maxEx - _minEx + 1);
        if (NO_ROW != enemy_row_to_kill)
        {
            _enemyChunk = KillRow(_enemyChunk, enemy_row_to_kill);
            
            AddRowsForPlayer(1);
            //MakeRandomAngryChunk();
        }
        else
        {
            
        }
        infoText.text = $"PTS:{_score}\t\tMAX:{_difficulty}\nCURRIC 576";
        _fixedUpdateCount = 1;
    }
    private void Awake()
    {
        _myPiece = null;
        _myChunk = null;
        Dirty = true;
        _initialized = false;
        InitializePieces();
    }
    void Start()
    {
       
        if (!_initialized) SetupBaseBoards();
    }
    void Update()
    {
        if (null == Camera.main) return; 
        if (!_initialized) SetupBaseBoards();
        if (null == _myPiece)
        {
            _myPiece = CreateAPiece((_minBx + _maxBx) / 2, _maxBy);
            if (!ValidPiece(_myPiece, true))
            {   
                Debug.Log("NO VALID MOVE");
                Debug.Break();
            }
        }
        
        
        if (Input.GetKeyDown(KeyCode.Q)) { Debug.Break(); }
        else if (Input.GetMouseButtonDown(0)) 
        {
            var point = Camera.main.ScreenToWorldPoint(Input.mousePosition); 
            var selectedTile = boardMap.WorldToCell(point);
            _myChunk = AddChunkAtPoint(_myChunk, selectedTile);
            // Debug.Log(selectedTile);
            // boardMap.SetTile(selectedTile, pieceTile); 
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) { PlayerDoLeft(); }
        else if (Input.GetKeyDown(KeyCode.RightArrow)) { PlayerDoRight(); }
        else if (Input.GetKeyDown(KeyCode.UpArrow)) { PlayerDoUp(); }
        else if (Input.GetKeyDown(KeyCode.DownArrow)) { PlayerDoDrop(); }

        if (!Dirty) return;
        DrawAllBoards();
        DrawAllPieces();
        Dirty = false;
    } 
    
   
}
