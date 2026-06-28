using UnityEngine;
using UnityEngine.AI;
public interface IState
{
    void Enter();
    void Execute();
    void Exit();
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "NPC/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Pistol";
    public float damage = 10f;
    public float fireRate = 0.5f;

    public int maxAmmo = 15;
    public float reloadTime = 2.0f;
}
public class Weapon
{
    public WeaponData data { get; private set; }

    int currentAmmo;
    float fireCooldown;
    float reloadTimer;
    bool isReloading;

    public bool IsReloading => isReloading;
    public bool IsEmpty => currentAmmo <= 0;
    public int CurrentAmmo => currentAmmo;

    // ── イベント ──────────────────────────────────
    public event System.Action<float> OnFired;         // 引数：ダメージ量
    public event System.Action OnReloadStarted;
    public event System.Action OnReloadFinished;
    // ─────────────────────────────────────────────

    public Weapon(WeaponData weaponData)
    {
        data = weaponData;
        currentAmmo = data.maxAmmo;
    }

    public void Update()
    {
        if (fireCooldown > 0f) fireCooldown -= Time.deltaTime;

        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                currentAmmo = data.maxAmmo;
                isReloading = false;
                OnReloadFinished?.Invoke();     // ← リロード完了
            }
        }
    }

    public bool TryFire(out float damage)
    {
        damage = 0f;
        if (isReloading || fireCooldown > 0f || IsEmpty) return false;

        currentAmmo--;
        fireCooldown = data.fireRate;
        damage = data.damage;
        OnFired?.Invoke(damage);                // ← 射撃発生

        if (IsEmpty) StartReload();
        return true;
    }

    public void StartReload()
    {
        if (isReloading || currentAmmo == data.maxAmmo) return;
        isReloading = true;
        reloadTimer = data.reloadTime;
        OnReloadStarted?.Invoke();              // ← リロード開始
    }
}
public class NPCController : MonoBehaviour
{
    [Header("参照")]
    public NavMeshAgent agent;
    public Transform enemy;
    public WeaponData weaponData;   // Inspectorでアセットをアサイン

    [Header("距離設定")]
    public float minDist = 3f;
    public float maxDist = 8f;
    public float strafeStep = 2f;

    [Header("HP設定")]
    public float hp = 100f;

    public Weapon weapon { get; private set; }
    IState currentState;

    void Start()
    {
        weapon = new Weapon(weaponData);
        ChangeState(new AttackState());
    }

    void Update()
    {
        weapon.Update();   // クールダウン・リロード更新
        currentState?.Execute();
    }

    // TryShootはWeaponに委譲
    public void TryShoot()
    {
        if (weapon.TryFire(out float damage))
        {
            Debug.Log($"射撃！ ダメージ:{damage} 残弾:{weapon.CurrentAmmo}");
            // enemy.GetComponent<Health>()?.TakeDamage(damage); など
        }
    }

    public void ChangeState(IState next)
    {
        currentState?.Exit();
        currentState = next;
        if (currentState is NPCState s) s.Init(this);
        currentState.Enter();
    }
}

// NPCStateの基底クラス（npcへの参照を共通で持つ）
public abstract class NPCState : IState
{
    protected NPCController npc;

    public void Init(NPCController controller)
    {
        npc = controller;
    }

    public virtual void Enter() { }
    public virtual void Execute() { }
    public virtual void Exit() { }
}

public class AttackState : NPCState
{
    enum SubState { KeepAway, Strafe, Hold }

    SubState subState;
    float strafeTimer;
    float strafeDir = 1f;
    const float STRAFE_FLIP_INTERVAL = 2.0f;

    bool canShoot = true;   // リロード中はfalse

    public override void Enter()
    {
        npc.weapon.OnFired += OnFire;
        npc.weapon.OnReloadStarted += OnReloadStart;
        npc.weapon.OnReloadFinished += OnReloadFinish;
    }

    public override void Exit()
    {
        npc.weapon.OnFired -= OnFire;
        npc.weapon.OnReloadStarted -= OnReloadStart;
        npc.weapon.OnReloadFinished -= OnReloadFinish;
    }

    // ── イベントハンドラ ──────────────────────────
    void OnFire(float damage)
    {
        // enemy.GetComponent<Health>()?.TakeDamage(damage);
    }

    void OnReloadStart()
    {
        canShoot = false;
    }

    void OnReloadFinish()
    {
        canShoot = true;
    }
    // ─────────────────────────────────────────────

    public override void Execute()
    {
        float dist = Vector3.Distance(npc.transform.position, npc.enemy.position);

        if (dist < npc.minDist) subState = SubState.KeepAway;
        else if (dist < npc.maxDist) subState = SubState.Strafe;
        else subState = SubState.Hold;

        switch (subState)
        {
            case SubState.KeepAway: MoveKeepAway(); break;
            case SubState.Strafe: MoveStrafe(); break;
            case SubState.Hold: npc.agent.ResetPath(); break;
        }

        // リロード中は射撃しない
        if (canShoot) npc.TryShoot();

        // 別ステート遷移チェック
        if (npc.hp < 10)
        { 
            // setState
        } 
    }

    void MoveKeepAway()
    {
        Vector3 fleeTarget = NPCPositionHelper.GetKeepAwayTarget(
            npc.transform.position,
            npc.enemy.position,
            npc.minDist
        );
        npc.agent.SetDestination(fleeTarget);
    }

    void MoveStrafe()
    {
        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDir *= -1f;
            strafeTimer = STRAFE_FLIP_INTERVAL;
        }

        Vector3 toEnemy = (npc.enemy.position - npc.transform.position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toEnemy);
        Vector3 target = npc.transform.position + right * strafeDir * npc.strafeStep;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            npc.agent.SetDestination(hit.position);
    }
}

public static class NPCPositionHelper
{
    /// <summary>
    /// 敵から最低距離を保つターゲット位置を返す
    /// </summary>
    public static Vector3 GetKeepAwayTarget(Vector3 npc, Vector3 enemy, float minDistance)
    {
        Vector3 toNpc = npc - enemy;
        float currentDistance = toNpc.magnitude;

        if (currentDistance >= minDistance)
            return npc;

        // ゼロベクトル対策：完全に重なった場合は固定方向（右）へ
        Vector3 dir = currentDistance > 0.001f ? toNpc.normalized : Vector3.right;

        return enemy + dir * minDistance;
    }
}