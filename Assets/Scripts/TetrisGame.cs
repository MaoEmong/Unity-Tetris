using System.Collections.Generic;
using TMPro;
using UnityEngine;

/*
========================================
TETRIS (Single Script / Clean Reset)
- PC
- 10x20
- Auto fit to resolution
- No bullshit
========================================
*/

public class TetrisGame : MonoBehaviour
{
    /* ===== SETTINGS ===== */
    public GameObject blockPrefab;
    public GameObject boardCellPrefab; // 반투명 검정 블록
    private Transform boardRoot;


    public int width = 10;
    public int height = 20;

    public float cellSize = 1f;
    public int paddingCells = 1;

    public float fallInterval = 0.8f;
    public float softDropMultiplier = 0.1f;

    [Header("Move Repeat (DAS/ARR)")]
    public float dasDelay = 0.15f;   // 누르고 있다가 반복 시작까지 대기
    public float arrInterval = 0.05f; // 반복 이동 간격(작을수록 촥촥)

    [Header("Next Preview")]
    public Vector2 nextPreviewWorldPos = new Vector2(8f, 6f); // 화면/보드 옆으로 적당히
    public float nextPreviewScale = 0.7f;

    [Header("Score")]
    public int score = 0;

    [Header("Level / Speed")]
    public int totalLinesCleared = 0;
    public int level = 1;

    [Header("Input Lock")]
    public float hardDropLockAfterSpawn = 0.12f; // 0.08~0.2 추천
    private float hardDropLockTimer = 0f;

    [Header("Game Start Delay")]
    public float startDelay = 3f;

    private float startTimer = 0f;
    private bool gameStarted = false;

    public TextMeshProUGUI scoreText;
    public TextMeshPro_OutlineObject startTimerText;

    // 레벨별 낙하 속도 테이블
    public float[] levelFallIntervals =
    {
    0.8f,  // Lv 1
    0.7f,  // Lv 2
    0.6f,  // Lv 3
    0.5f,  // Lv 4
    0.4f,  // Lv 5
    0.3f,  // Lv 6
    0.22f, // Lv 7
    0.16f, // Lv 8
    0.12f, // Lv 9
    0.1f   // Lv 10+
};
    public int linesPerLevel = 10;

    public float minFallInterval = 0.08f;
    public float fallDecreasePerLevel = 0.06f; // 레벨당 감소량(취향)



    /* ===== INTERNAL ===== */
    private Transform[,] grid;
    private Piece current;
    private float fallTimer;

    private int holdDir = 0;         // -1, 0, 1
    private float holdTime = 0f;
    private float repeatTimer = 0f;
    private PieceType nextType;
    private Transform[] nextBlocks = new Transform[4];

    private Queue<PieceType> bag = new Queue<PieceType>();
    private System.Random rng = new System.Random();

    private Vector2 boardOrigin;

    private Transform[] ghostBlocks = new Transform[4];

    private CameraShake cameraShake;

    /* ===== UNITY ===== */
    void Awake()
    {
        grid = new Transform[width, height];
        FillBag();
        FillBag();
    }

    void Start()
    {
        cameraShake = Camera.main.GetComponent<CameraShake>();
        AutoFitCamera();
        BuildBoardVisual();

        nextType = PopNextType();
        BuildNextPreviewVisual();
        UpdateNextPreviewVisual();

        startTimer = startDelay;
        gameStarted = false;
    }

    void Update()
    {
        if (!gameStarted)
        {
            startTimer -= Time.deltaTime;
            startTimerText.SetText(Mathf.CeilToInt(startTimer).ToString());

            if (startTimer <= 0f)
            {
                gameStarted = true;
                startTimerText.SetText("");
                SpawnNext();
            }
            return; // 시작 전엔 입력/낙하 전부 막음
        }
        if (current == null) return;

        if (hardDropLockTimer > 0f)
            hardDropLockTimer -= Time.deltaTime;

        HandleInput();

        float interval = fallInterval;
        if (Input.GetKey(KeyCode.DownArrow))
            interval *= softDropMultiplier;

        fallTimer += Time.deltaTime;
        if (fallTimer >= interval)
        {
            fallTimer = 0f;
            StepDown();
        }
    }

    /* ===== INPUT ===== */
    void HandleInput()
    {
        // ----- 좌/우 홀드 반복 이동 -----
        int dir = 0;
        if (Input.GetKey(KeyCode.LeftArrow)) dir = -1;
        else if (Input.GetKey(KeyCode.RightArrow)) dir = 1;

        if (dir == 0)
        {
            holdDir = 0;
            holdTime = 0f;
            repeatTimer = 0f;
        }
        else
        {
            if (holdDir != dir)
            {
                // 방향이 바뀌거나 새로 눌림: 즉시 1칸
                holdDir = dir;
                holdTime = 0f;
                repeatTimer = 0f;
                TryMove(dir, 0);
            }
            else
            {
                // 계속 누르고 있음: DAS 이후 ARR로 반복 이동
                holdTime += Time.deltaTime;

                if (holdTime >= dasDelay)
                {
                    repeatTimer += Time.deltaTime;
                    while (repeatTimer >= arrInterval)
                    {
                        repeatTimer -= arrInterval;
                        if (!TryMove(dir, 0))
                            break; // 벽/블록 막히면 그만
                    }
                }
            }
        }

        // ----- 회전 / 하드드롭 -----
        if (Input.GetKeyDown(KeyCode.Z)) TryRotate(-1);
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.UpArrow)) TryRotate(1);
        if (hardDropLockTimer <= 0f && Input.GetKeyDown(KeyCode.Space))
            HardDrop();
    }


    /* ===== GAMEPLAY ===== */
    void StepDown()
    {
        if (!TryMove(0, -1))
        {
            LockPiece();
            ClearLines();
            SpawnNext();
        }
    }

    void HardDrop()
    {
        int dropCount = 0;
        while (TryMove(0, -1))
            dropCount++;

        score += dropCount * 2; // 하드 드롭 보너스

        LockPiece();
        ClearLines();
        SpawnNext();
    }

    bool TryMove(int dx, int dy)
    {
        Vector2Int np = current.pos + new Vector2Int(dx, dy);
        if (!IsValid(current.type, np, current.rot)) return false;

        current.pos = np;
        ApplyPiece();
        UpdateGhost();
        return true;
    }

    void TryRotate(int dir)
    {
        if (current.type == PieceType.O)
            return;

        int nr = (current.rot + dir) & 3;

        Vector2Int[] kicks;

        if (current.type == PieceType.I)
        {
            kicks = new Vector2Int[]
            {
                new Vector2Int(0,0),
                new Vector2Int(2,0),
                new Vector2Int(-2,0),
                new Vector2Int(1,0),
                new Vector2Int(-1,0),
                new Vector2Int(0,1),
                new Vector2Int(0,-1),
            };
        }
        else
        {
            kicks = new Vector2Int[]
            {
                new Vector2Int(0,0),
                new Vector2Int(1,0),
                new Vector2Int(-1,0),
                new Vector2Int(0,1)
            };
        }

        foreach (var k in kicks)
        {
            var np = current.pos + k;
            if (IsValid(current.type, np, nr))
            {
                current.pos = np;
                current.rot = nr;
                ApplyPiece();
                UpdateGhost();
                return;
            }
        }
    }

    void LockPiece()
    {
        foreach (var c in GetCells(current.type, current.pos, current.rot))
            grid[c.x, c.y] = current.blocks[c.i];

        current = null;

        for (int i = 0; i < ghostBlocks.Length; i++)
        {
            if (ghostBlocks[i] != null)
            {
                Destroy(ghostBlocks[i].gameObject);
                ghostBlocks[i] = null; // ✅ 이게 핵심
            }
        }

        cameraShake?.Shake(0.08f, 0.08f, priority: 1);
    }

    void ClearLines()
    {
        int clearedCount = 0;

        for (int y = 0; y < height; y++)
        {
            bool full = true;
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] == null)
                {
                    full = false;
                    break;
                }
            }

            if (!full) continue;

            clearedCount++;

            for (int x = 0; x < width; x++)
            {
                Destroy(grid[x, y].gameObject);
                grid[x, y] = null;
            }

            for (int yy = y + 1; yy < height; yy++)
            {
                for (int x = 0; x < width; x++)
                {
                    grid[x, yy - 1] = grid[x, yy];
                    grid[x, yy] = null;

                    if (grid[x, yy - 1] != null)
                        grid[x, yy - 1].position += Vector3.down * cellSize;
                }
            }

            y--;
        }

        // 점수 처리
        if (clearedCount > 0)
        {
            AddLineScore(clearedCount);

            totalLinesCleared += clearedCount;
            UpdateLevelAndSpeed();

            cameraShake?.Shake(0.15f, 0.2f, priority: 2);
        }
    }

    /* ===== SPAWN ===== */
    void SpawnNext()
    {
        // 현재는 nextType으로 스폰
        PieceType t = nextType;
        Spawn(t);

        // 다음 nextType 갱신 + 프리뷰 갱신
        nextType = PopNextType();
        UpdateNextPreviewVisual();

        hardDropLockTimer = hardDropLockAfterSpawn;
    }

    void Spawn(PieceType t)
    {
        current = new Piece
        {
            type = t,
            pos = new Vector2Int(width / 2, height - 2),
            rot = 0,
            blocks = new Transform[4]
        };

        Color c = GetPieceColor(t);

        for (int i = 0; i < 4; i++)
        {
            var tr = Instantiate(blockPrefab, transform).transform;
            current.blocks[i] = tr;

            var sr = tr.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = c;
                sr.sortingOrder = 10;
            }
        }

        // 고스트가 없으면 생성
        for (int i = 0; i < 4; i++)
        {
            if (ghostBlocks[i] == null)
            {
                var g = Instantiate(blockPrefab, transform).transform;
                ghostBlocks[i] = g;

                var sr = g.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(c.r, c.g, c.b, 0.5f);
                    sr.sortingOrder = 5;
                }
            }
            ghostBlocks[i].localScale = Vector3.one;
        }

        if (!IsValid(t, current.pos, current.rot))
        {
            Debug.Log("GAME OVER");

            // ✅ 점수 전달
            if (GameSession.I != null)
            {
                GameSession.I.SetScore(score);
                GameSession.I.SubmitRankingIfPossible();
            }

            enabled = false; // 게임 정지
            return;
        }

        ApplyPiece();
        UpdateGhost();
    }

    PieceType PopNextType()
    {
        if (bag.Count < 7) FillBag();
        return bag.Dequeue();
    }

    void BuildNextPreviewVisual()
    {
        for (int i = 0; i < 4; i++)
        {
            var tr = Instantiate(blockPrefab, transform).transform;
            nextBlocks[i] = tr;
            tr.localScale = Vector3.one * nextPreviewScale;

            var sr = tr.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 50; // 위에 뜨게
        }
    }

    void UpdateNextPreviewVisual()
    {
        var offs = Shape(nextType);

        Color c = GetPieceColor(nextType);

        for (int i = 0; i < 4; i++)
        {
            var o = offs[i]; // rot 0 고정
            nextBlocks[i].position = new Vector3(
                nextPreviewWorldPos.x + o.x * cellSize * nextPreviewScale,
                nextPreviewWorldPos.y + o.y * cellSize * nextPreviewScale,
                0f
            );

            var sr = nextBlocks[i].GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = c;
        }
    }

    void UpdateGhost()
    {
        if (current == null) return;

        Vector2Int ghostPos = current.pos;

        // 바닥까지 내려보냄
        while (IsValid(current.type, ghostPos + Vector2Int.down, current.rot))
            ghostPos += Vector2Int.down;

        var offsets = Shape(current.type);

        for (int i = 0; i < 4; i++)
        {
            if (ghostBlocks[i] == null)
            {
                var g = Instantiate(blockPrefab, transform).transform;
                ghostBlocks[i] = g;

                var sr = g.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    var c = GetPieceColor(current.type);
                    sr.color = new Color(c.r, c.g, c.b, 0.25f);
                    sr.sortingOrder = 5;
                }
            }

            var o = Rotate(offsets[i], current.rot);
            ghostBlocks[i].position = new Vector3(
                boardOrigin.x + (ghostPos.x + o.x) * cellSize,
                boardOrigin.y + (ghostPos.y + o.y) * cellSize,
                0f
            );
        }
    }
    void FillBag()
    {
        var list = new List<PieceType>
        { PieceType.I, PieceType.O, PieceType.T,
          PieceType.S, PieceType.Z, PieceType.J, PieceType.L };

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        foreach (var t in list) bag.Enqueue(t);
    }

    /* ===== CORE ===== */
    void ApplyPiece()
    {
        var offs = Shape(current.type);

        for (int i = 0; i < 4; i++)
        {
            var o = Rotate(offs[i], current.rot);
            current.blocks[i].position =
                new Vector3(
                    boardOrigin.x + (current.pos.x + o.x) * cellSize,
                    boardOrigin.y + (current.pos.y + o.y) * cellSize,
                    0f);
        }
    }

    bool IsValid(PieceType t, Vector2Int p, int r)
    {
        foreach (var c in GetCells(t, p, r))
        {
            if (c.x < 0 || c.x >= width || c.y < 0 || c.y >= height)
                return false;
            if (grid[c.x, c.y] != null)
                return false;
        }
        return true;
    }
    void UpdateLevelAndSpeed()
    {
        level = (totalLinesCleared / 10) + 1;

        int idx = Mathf.Min(level - 1, levelFallIntervals.Length - 1);
        fallInterval = levelFallIntervals[idx];

        Debug.Log($"Level {level} | Lines {totalLinesCleared} | Fall {fallInterval}");
    }
    void BuildBoardVisual()
    {
        if (boardCellPrefab == null) return; // 프리팹 안 넣으면 그냥 스킵

        if (boardRoot != null) Destroy(boardRoot.gameObject);
        boardRoot = new GameObject("BoardVisual").transform;
        boardRoot.SetParent(transform, false);

        // 배경은 블록보다 뒤로 가게 z를 +1 또는 -1로 조절
        // (2D는 SortingLayer가 베스트지만, 지금은 z로만 간단히)
        float z = 1f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var t = Instantiate(boardCellPrefab, boardRoot).transform;
                t.name = $"Cell_{x}_{y}";

                t.position = new Vector3(
                    boardOrigin.x + x * cellSize,
                    boardOrigin.y + y * cellSize,
                    z
                );
            }
        }
    }
    void AddLineScore(int lines)
    {
        int add = lines switch
        {
            1 => 100,
            2 => 300,
            3 => 500,
            4 => 800,
            _ => 0
        };

        score += add;
        scoreText.SetText(score.ToString());
        Debug.Log($"Score +{add}  (Total: {score})");
    }

    struct Cell { public int x, y, i; }

    List<Cell> GetCells(PieceType t, Vector2Int p, int r)
    {
        var offs = Shape(t);
        var list = new List<Cell>(4);
        for (int i = 0; i < 4; i++)
        {
            var o = Rotate(offs[i], r);
            list.Add(new Cell { x = p.x + o.x, y = p.y + o.y, i = i });
        }
        return list;
    }

    Vector2Int Rotate(Vector2Int v, int r)
    {
        r &= 3;
        if (r == 1) return new Vector2Int(-v.y, v.x);
        if (r == 2) return new Vector2Int(-v.x, -v.y);
        if (r == 3) return new Vector2Int(v.y, -v.x);
        return v;
    }

    Vector2Int[] Shape(PieceType t)
    {
        return t switch
        {
            PieceType.I => new Vector2Int[]
            {
            new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0)
            },
            PieceType.O => new Vector2Int[]
            {
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1)
            },
            PieceType.T => new Vector2Int[]
            {
            new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1)
            },
            PieceType.S => new Vector2Int[]
            {
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(-1,1), new Vector2Int(0,1)
            },
            PieceType.Z => new Vector2Int[]
            {
            new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1)
            },
            PieceType.J => new Vector2Int[]
            {
            new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(-1,1)
            },
            _ => new Vector2Int[]
            {
            new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1)
            },
        };
    }

    Color GetPieceColor(PieceType t)
    {
        // 정통 테트리스 느낌 색
        return t switch
        {
            PieceType.I => new Color(0.0f, 0.9f, 0.9f, 1f), // cyan
            PieceType.O => new Color(0.95f, 0.9f, 0.0f, 1f), // yellow
            PieceType.T => new Color(0.7f, 0.2f, 0.9f, 1f), // purple
            PieceType.S => new Color(0.1f, 0.9f, 0.1f, 1f), // green
            PieceType.Z => new Color(0.9f, 0.1f, 0.1f, 1f), // red
            PieceType.J => new Color(0.1f, 0.2f, 0.9f, 1f), // blue
            _ => new Color(0.95f, 0.55f, 0.1f, 1f), // orange (L)
        };
    }

    void AutoFitCamera()
    {
        var cam = Camera.main;
        cam.orthographic = true;

        float bw = (width + paddingCells * 2) * cellSize;
        float bh = (height + paddingCells * 2) * cellSize;

        cam.orthographicSize =
            Mathf.Max(bh / 2f, (bw / cam.aspect) / 2f);

        cam.transform.position = new Vector3(0, 0, -10);

        boardOrigin = new Vector2(
            -(width - 1) * cellSize / 2f,
            -(height - 1) * cellSize / 2f);
    }

    enum PieceType { I, O, T, S, Z, J, L }

    class Piece
    {
        public PieceType type;
        public Vector2Int pos;
        public int rot;
        public Transform[] blocks;
    }
}
