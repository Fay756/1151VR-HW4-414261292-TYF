using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class CarFlightController : MonoBehaviour
{
    [Header("1. 目標障礙物設定")]
    [Tooltip("請把場景中的紫色方塊 (Cube) 拖曳到此處")]
    public Collider obstacleCollider;

    [Header("2. 即時距離監控 (可於 Inspector 觀看)")]
    [Tooltip("車子與障礙物的即時 3D 直線距離 (公尺)")]
    public float distanceToObstacle;

    [Tooltip("車子與障礙物的水平地面距離 (不考慮車子飛多高)")]
    public float horizontalDistance;

    [Header("3. 車輛移動與升空設定")]
    public float forwardSpeed = 12f;   // 前進速度
    public float flyForce = 15f;       // 向上升空的力道

    [Header("4. 音效剪輯 (Audio Clips)")]
    public AudioClip flightSound;      // 飛行中音效 (循環播放)
    public AudioClip crashSound;       // 撞擊障礙物音效
    public AudioClip passSound;        // 成功飛越上方音效

    // 內部元件與狀態變數
    private Rigidbody rb;
    private AudioSource audioSource;
    private bool isFlying = false;
    private bool hasCrashed = false;
    private bool hasPassed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // 確保初始時不會自動播放
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        // 1. 計算與障礙物的距離
        UpdateDistance();

        // 若已撞毀，則停止後續操作與判定
        if (hasCrashed) return;

        // 2. 處理空白鍵飛行操作與飛行音效
        HandleFlightInput();

        // 3. 以座標比對：判定是否從障礙物上方飛過
        CheckFlyOverObstacle();
    }

    void FixedUpdate()
    {
        // 車子持續向前移動 (物理移動)
        if (!hasCrashed)
        {
            Vector3 forwardMove = transform.forward * forwardSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + forwardMove);
        }
    }

    // 計算距離邏輯
    void UpdateDistance()
    {
        if (obstacleCollider != null)
        {
            Vector3 carPos = transform.position;
            Vector3 obsPos = obstacleCollider.transform.position;

            // 3D 直線距離
            distanceToObstacle = Vector3.Distance(carPos, obsPos);

            // 水平地面距離 (忽略高度 Y 軸)
            Vector2 carXZ = new Vector2(carPos.x, carPos.z);
            Vector2 obsXZ = new Vector2(obsPos.x, obsPos.z);
            horizontalDistance = Vector2.Distance(carXZ, obsXZ);
        }
    }

    // 空白鍵升空與音效控制
    void HandleFlightInput()
    {
        // 新版輸入系統的空白鍵按壓檢查
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        if (spacePressed)
        {
            rb.AddForce(Vector3.up * flyForce, ForceMode.Acceleration);

            if (!isFlying && flightSound != null)
            {
                isFlying = true;
                audioSource.clip = flightSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            if (isFlying)
            {
                isFlying = false;
                audioSource.Stop();
                audioSource.loop = false;
            }
        }
    }

    // 座標判定：檢查車子是否在方塊正上方且高於方塊頂部
    void CheckFlyOverObstacle()
    {
        if (obstacleCollider == null || hasPassed || hasCrashed) return;

        Bounds b = obstacleCollider.bounds;
        Vector3 carPos = transform.position;

        // 容許緩衝範圍 (公尺)，避免車速快時單一幀漏掉偵測
        float margin = 1.0f;

        // 條件 A：車子處於障礙物的水平投影範圍內 (X 與 Z 軸)
        bool insideHorizontal = (carPos.x >= b.min.x - margin && carPos.x <= b.max.x + margin) &&
                                (carPos.z >= b.min.z - margin && carPos.z <= b.max.z + margin);

        // 條件 B：車子高度高於障礙物頂端 (Y 軸)
        bool isAbove = carPos.y > b.max.y;

        // 兩者皆滿足，代表成功飛越！
        if (insideHorizontal && isAbove)
        {
            hasPassed = true;

            // 播放單次成功音效 (PlayOneShot 不會打斷正在播放的飛行循環音)
            if (passSound != null)
            {
                audioSource.PlayOneShot(passSound);
            }

            Debug.Log("【成功】順利飛過障礙物上方！");
        }
    }

    // 實體碰撞：撞擊障礙物
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") && !hasCrashed)
        {
            hasCrashed = true;
            isFlying = false;

            // 停止飛行循環音，播放撞擊音效
            audioSource.Stop();
            audioSource.loop = false;

            if (crashSound != null)
            {
                audioSource.PlayOneShot(crashSound);
            }

            Debug.Log("【失敗】撞上障礙物！");
        }
    }
    void OnGUI()
    {
        // 1. 左上角即時距離
        GUIStyle distStyle = new GUIStyle();
        distStyle.fontSize = 32;
        distStyle.normal.textColor = Color.yellow;
        distStyle.fontStyle = FontStyle.Bold;

        string displayText = $"距離障礙物：{distanceToObstacle:F1} 公尺";
        GUI.Label(new Rect(30, 30, 400, 60), displayText, distStyle);

        // 2. 成功飛過時：螢幕正中央顯示綠色大字 CLEAR!!
        if (hasPassed)
        {
            GUIStyle clearStyle = new GUIStyle();
            clearStyle.fontSize = 80;                      // 超大字體
            clearStyle.normal.textColor = Color.green;     // 綠色
            clearStyle.fontStyle = FontStyle.Bold;
            clearStyle.alignment = TextAnchor.MiddleCenter; // 文字置中

            // 畫在螢幕正中央
            float centerX = Screen.width / 2f - 250f;
            float centerY = Screen.height / 2f - 100f;
            GUI.Label(new Rect(centerX, centerY, 500, 200), "CLEAR!!", clearStyle);
        }

        // 3. 撞到障礙物時：螢幕正中央顯示紅色 CRASH!! (附帶功能)
        if (hasCrashed)
        {
            GUIStyle crashStyle = new GUIStyle();
            crashStyle.fontSize = 80;
            crashStyle.normal.textColor = Color.red;
            crashStyle.fontStyle = FontStyle.Bold;
            crashStyle.alignment = TextAnchor.MiddleCenter;

            float centerX = Screen.width / 2f - 250f;
            float centerY = Screen.height / 2f - 100f;
            GUI.Label(new Rect(centerX, centerY, 500, 200), "CRASH!!", crashStyle);
        }
    }
}