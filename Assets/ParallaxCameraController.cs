using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ParallaxCameraController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 2f;
    public Transform endTarget;
    public float maxMoveTime = 30f;

    [Header("碰撞反應")]
    public float pauseDuration = 1f;
    public float shakeIntensity = 0.3f;
    public float shakeDuration = 0.4f;
    public AudioClip hitSound;
    public AudioSource audioSource;

    [Header("模糊特效 (URP Volume)")]
    public Volume postProcessingVolume;  // 拖進有 DepthOfField 的 Volume
    private DepthOfField dof;            // 模糊控制元件
    public float blurIntensity = 3f;     // 模糊強度
    public float blurSpeed = 5f;         // 模糊變化速度

    private bool isPaused = false;
    private float moveTimer = 0f;
    private Vector3 originalPos;
    private float targetBlur = 0f;

    void Start()
    {
        originalPos = transform.position;

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 嘗試取得 DepthOfField 元件
        if (postProcessingVolume != null)
            postProcessingVolume.profile.TryGet(out dof);
    }

    void Update()
    {
        // 模糊動畫
        // if (dof != null)
        // {
        //     dof.gaussianEnd = Mathf.Lerp(dof.gaussianEnd, targetBlur, Time.deltaTime * blurSpeed);
        // }

        if (isPaused) return;

        moveTimer += Time.deltaTime;
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);

        if (endTarget != null && transform.position.z >= endTarget.position.z)
        {
            StopMovement();
        }
        else if (moveTimer >= maxMoveTime)
        {
            StopMovement();
        }
    }

    void StopMovement()
    {
        moveSpeed = 0;
        Debug.Log("📍鏡頭已停止！");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HitObject"))
        {
            StartCoroutine(HitReaction());
        }
    }

    IEnumerator HitReaction()
    {
        isPaused = true;
        Vector3 startPos = transform.position;

        // 播放音效
        if (hitSound != null)
            audioSource.PlayOneShot(hitSound);

        // 模糊 & 晃動
        targetBlur = blurIntensity;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
            float offsetY = Random.Range(-shakeIntensity, shakeIntensity);
            transform.position = startPos + new Vector3(offsetX, offsetY, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos;

        // 暫停
        yield return new WaitForSeconds(pauseDuration);

        // 恢復
        targetBlur = 0f;
        isPaused = false;
    }
}
