using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FusionHelpers;
using FusionGame.Stickman;
using Fusion;
using static FusionGame.Stickman.Player;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using static FusionHelpers.TickAlignedEventRelay;

/*
	Documentation: https://mirror-networking.com/docs/Guides/NetworkBehaviour.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/
namespace FusionGame.Stickman
{
    public class Player : FusionPlayer
    {
        public enum Stage
        {
            New,
            TeleportOut,
            TeleportIn,
            Active,
            Dead
        }

        // COMPONENTS
        [SerializeField] private Rigidbody rb = null;
        [SerializeField] private Animator anim;
        public CameraRotator playerCamera = null;

        [Header("Movement")]
        [SerializeField] private LayerMask walkableSurface;
        [SerializeField] private float speed = 10.0f;
        [SerializeField] private float maxSpeed = 10.0f;
        [SerializeField] private float counterMoveScale = 0.1f;
        [SerializeField] private float slopeThreshold = 0.2f;
        [SerializeField] private float gravForceMultiplier = 20.0f;
        [SerializeField] private float collisionForceThreshold = 50.0f;
        private float rotationAngle = 0.0f;

        private Vector3 inputDir = Vector3.zero;
        private Vector3 targetDir = Vector3.zero;
        private bool isGrounded = false;
        private bool playerStable = true;
        private bool playerControl = true;
        private bool hasFallenOver = false;
        private bool smacked = false;

        [Header("Ability Values")]
        [SerializeField] private float jumpForce = 5.0f;
        [SerializeField] private float jumpCheckDist = 0.25f;
        [SerializeField] private float diveWaitTime = 1.0f;
        [SerializeField] private Vector3 diveForce = Vector3.forward;
        [SerializeField] private float diveTorque = 90.0f;
        [SerializeField, Min(0.1f)] private float diveRecoveryMaxSpeed = 2.0f;
        [SerializeField] private float stableHitThreshold = 5.0f;

        [SerializeField] float turnSmoothVelocity;
        public float turnSmoothTime = 0.1f;
        Vector3 currentVelocity = Vector3.zero;
        [SerializeField] private TankTeleportInEffect _teleportIn;
        [SerializeField] private TankTeleportOutEffect _teleportOutPrefab;

        [SerializeField] private float _respawnTime;
        [Networked] public Stage stage { get; set; }
        [Networked] private TickTimer respawnTimer { get; set; }
        [Networked] public bool ready { get; set; }

        [Networked]
        public bool isJumping { get; set; } = false;
        private bool isJumpingDetected = false; // For dourble jumping
        [Networked]
        public bool isRunning { get; set; } = false;
        [Networked]
        public bool isVictory { get; set; } = false;
        [Networked]
        public int skin_id { get; set; } = -1;
        public int prev_skin = -1;

        public Renderer character_renderer;
        public List<CharacterTexture> character_textures;

        public GameObject MineSymbol;
        [SerializeField] private PowerupManager powerupManager;

        public Vector3 last_point = Vector3.zero;
        private float _respawnInSeconds = -1;
        private ChangeDetector _changes;
        private NetworkInputData _oldInput;
        public bool isActivated => (gameObject.activeInHierarchy && (stage == Stage.Active || stage == Stage.TeleportIn));
        public bool isRespawningDone => stage == Stage.TeleportIn && respawnTimer.Expired(Runner);

        public bool DebugLog = false;

        public void ToggleReady()
        {
            ready = !ready;
        }

        public void ResetReady()
        {
            ready = false;
        }
        public override void InitNetworkState()
        {
            stage = Stage.New;
        }

        public override void Spawned()
        {
            base.Spawned();

            DontDestroyOnLoad(gameObject);

            rb.useGravity = true;
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

            if (Object.HasInputAuthority)
            {
                MineSymbol.SetActive(true);

                skin_id = PlayerPrefs.GetInt("Char_SKIN_ID", 0);
                prev_skin = skin_id;
            }
            if (skin_id >= 0 && skin_id < character_textures.Count)
            {
                character_renderer.material.SetTexture("_MainTex", character_textures[skin_id].Main);
                //character_renderer.material.SetTexture("_MetallicGlossMap", character_textures[index].Metalic);
                //character_renderer.material.SetTexture("_BumpMap", character_textures[index].Normal);
            }

            ready = false;

            //_teleportIn.Initialize(this);

            // Proxies may not be in state "NEW" when they spawn, so make sure we handle the state properly, regardless of what it is
            OnStageChanged();

            _respawnInSeconds = 0;
            rotationAngle = transform.eulerAngles.y;

            RegisterEventListener((TickAlignedEventRelay.PickupEvent evt) => OnPickup(evt));
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            SpawnTeleportOutFx();
        }

        private void OnPickup(PickupEvent evt)
        {
            PowerupElement powerup = PowerupManager.GetPowerup(evt.powerup);

            powerupManager.InstallPowerup(powerup);
        }

        void Awake()
        {
        }

        public override void FixedUpdateNetwork()
        {
            if (Object.HasInputAuthority && playerCamera == null)
            {
                playerCamera = FindObjectOfType<CameraRotator>();
                if (playerCamera != null)
                    playerCamera.cameraTarget = transform;
            }

            // Get our input struct and act accordingly. This method will only return data if we
            // have Input or State Authority - meaning on the controlling player or the server.
            if (GetInput(out NetworkInputData input))
            {
                // We don't want to predict this because it's a toggle and a mis-prediction due to lost input will double toggle the button
                if (Object.HasStateAuthority && input.WasPressed(NetworkInputData.BUTTON_TOGGLE_READY, _oldInput))
                    ToggleReady();

                if (InputController.fetchInput)
                {
                    inputDir = new Vector3(input.moveDirection.x, 0, input.moveDirection.y).normalized;

                    // Gravity Multiplier - increased vertical accuracy
                    rb.AddForce(Vector3.down * Runner.DeltaTime * 10);
                    isGrounded = GroundCheck();

                    // DIVE
                    /*if(Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        playerControl = false;
                        playerStable = false;
                        anim.SetBool("Stable", playerStable);
                        anim.SetTrigger("Dive");

                        rb.velocity = Vector3.zero;
                        rb.AddForce(transform.rotation * diveForce, ForceMode.Impulse);
                        rb.AddRelativeTorque(new Vector3(diveTorque, 0,0), ForceMode.Impulse);
                    }*/

                    // JUMP
                    if (input.IsDown(NetworkInputData.BUTTON_JUMP) && isGrounded)
                    {
                        if (!isJumpingDetected)
                        {
                            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                            isJumping = true;
                            isJumpingDetected = true;
                            anim.SetBool("isJumping", isJumping);
                        }
                    }
                    else
                    {
                        Move(input.cameraRotation);
                    }

                    // Constrain rotation when stable
                    //if (playerStable)
                    //    rb.MoveRotation(Quaternion.Euler(0, rotationAngle, 0));

                    _oldInput = input;
                }

                if (Object.HasStateAuthority && input.Skin >= 0 && input.Skin != skin_id)
                {
                    skin_id = input.Skin;
                    prev_skin = skin_id;
                    if (skin_id >= 0 && skin_id < character_textures.Count)
                    {
                        character_renderer.material.SetTexture("_MainTex", character_textures[skin_id].Main);
                        //character_renderer.material.SetTexture("_MetallicGlossMap", character_textures[index].Metalic);
                        //character_renderer.material.SetTexture("_BumpMap", character_textures[index].Normal);
                    }
                }
            }

            if (Object.HasStateAuthority)
            {
                CheckRespawn();

                if (isRespawningDone)
                    ResetPlayer();
            }
        }
 
        /// <summary>
        /// Render is the Fusion equivalent of Unity's Update() and unlike FixedUpdateNetwork which is very different from FixedUpdate,
        /// Render is in fact exactly the same. It even uses the same Time.deltaTime time steps. The purpose of Render is that
        /// it is always called *after* FixedUpdateNetwork - so to be safe you should use Render over Update if you're on a
        /// SimulationBehaviour.
        ///
        /// Here, we use Render to update visual aspects of the Tank that does not involve changing of networked properties.
        /// </summary>
        public override void Render()
        {
            foreach (var change in _changes.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(stage):
                        OnStageChanged();
                        break;
                    case nameof(isRunning):
                        anim.SetBool("isRunning", isRunning);
                        break;
                    case nameof(isJumping):
                        anim.SetBool("isJumping", isJumping);
                        break;
                    case nameof(isVictory):
                        anim.SetBool("isVictory", isVictory);
                        break;
                    case nameof(skin_id):
                        if (skin_id >= 0)
                            prev_skin = skin_id;
                        else
                        {
                            if (prev_skin >= 0)
                                skin_id = prev_skin;
                            else if (Object.HasInputAuthority)
                            {
                                skin_id = PlayerPrefs.GetInt("Char_SKIN_ID", 0);
                                prev_skin = skin_id;
                            }
                        }

                        if (skin_id >= 0 && skin_id < character_textures.Count)
                        {
                            character_renderer.material.SetTexture("_MainTex", character_textures[skin_id].Main);
                            //character_renderer.material.SetTexture("_MetallicGlossMap", character_textures[index].Metalic);
                            //character_renderer.material.SetTexture("_BumpMap", character_textures[index].Normal);
                        }
                        break;
                }
            }
        }

        public void SetVictory(bool vic)
        {
            isVictory = vic;
            anim.SetBool("isVictory", isVictory);
        }

        /// <summary>
        /// Handles Movement
        /// </summary>
        private void Move(float cameraRotation)
        {
            //Debug.DrawRay(transform.position, transform.forward * 10, Color.blue);
            if (inputDir.magnitude > 0.1f)
            {
                // Rotation
                float tempAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraRotation;
                targetDir = (Quaternion.Euler(0f, tempAngle, 0f) * Vector3.forward).normalized;
                rotationAngle = Mathf.SmoothDampAngle(rotationAngle, tempAngle, ref turnSmoothVelocity, turnSmoothTime, Mathf.Infinity, Runner.DeltaTime);
                inputDir = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;

                // Movement
                if (isGrounded && !hasFallenOver)
                {
                    if (rb.velocity.magnitude < getSpeed(maxSpeed) && Vector3.Dot(transform.forward, targetDir) > 0.95f)
                        rb.AddForce(transform.forward * getSpeed(speed), ForceMode.Force);// rb.AddForce(inputDir * getSpeed(speed), ForceMode.Force);
                }
                isRunning = true;
                anim.SetBool("isRunning", isRunning);

                if (isGrounded)
                {
                    isJumping = false;
                    isJumpingDetected = false;
                    anim.SetBool("isJumping", isJumping);
                }
            }
            else if (isGrounded)
            {
                // Counter Movement & Sliding Prevention (not particularly great method)
                rb.AddForce(getSpeed(speed) * Vector3.forward * Runner.DeltaTime * -rb.velocity.z * counterMoveScale);
                rb.AddForce(getSpeed(speed) * Vector3.right * Runner.DeltaTime * -rb.velocity.x * counterMoveScale);

                isJumping = false;
                isJumpingDetected = false;
                isRunning = false;

                anim.SetBool("isRunning", isRunning);
                anim.SetBool("isJumping", isJumping);
            }
            if (playerStable)
                rb.MoveRotation(Quaternion.Euler(0, rotationAngle, 0));// transform.rotation = Quaternion.Euler(0f, rotationAngle, 0f);//
        }

        private float getSpeed(float sp)
        {
            if (powerupManager.IsInstalled(PowerupType.SPEED_UP))
                return sp * PowerupManager.GetPowerup(PowerupManager.GetPowerupIndex(PowerupType.SPEED_UP)).value;
            else
                return sp;
        }

        /// <summary>
        /// Returns whether player is stood on Ground/Walkable Surface
        /// </summary>
        private bool GroundCheck()
        {
            RaycastHit hit;
            Physics.Raycast(transform.position, Vector3.down, out hit, jumpCheckDist, walkableSurface);

            if (hit.transform)
                return true;
            else
                return false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!smacked)
            {
                float collisionForce = collision.impulse.magnitude;
                if (collisionForce > collisionForceThreshold)
                {
                    playerStable = false;
                    playerControl = false;
                    rb.AddForce(collision.impulse * 0.25f, ForceMode.Impulse);
                }
            }

        }

        IEnumerator SetParent(Transform parent)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.rotation = Quaternion.Euler(new Vector3(0, transform.eulerAngles.y, transform.eulerAngles.z));

            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();

            this.transform.parent = parent;
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!hasFallenOver && !collision.transform.CompareTag("Player"))
            {
                HandleHasFallenOver();
            }
        }

        /// <summary>
        /// Handles fallen over check and resulting logic.
        /// </summary>
        private void HandleHasFallenOver()
        {
            // Fallen over Check
            if (Vector3.Dot(Vector3.down, transform.up) > -0.2f)
            {
                hasFallenOver = true;
                StartCoroutine(StandUp());
            }
        }

        /// <summary>
        /// Instantly returns Player back to standing after lying delay.
        /// </summary>
        private IEnumerator StandUp()
        {
            // Delay...
            yield return new WaitForSeconds(diveWaitTime);

            // Wait until speed while fallen is slowed
            yield return new WaitUntil(() => rb.velocity.magnitude < diveRecoveryMaxSpeed);

            // Reset pos + rot
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = transform.position + Vector3.up * 0.6f;
            rb.rotation = Quaternion.Euler(new Vector3(0, transform.eulerAngles.y, transform.eulerAngles.z));

            hasFallenOver = false;
            playerControl = true;
            playerStable = true;
            smacked = false;
            anim.SetBool("Stable", playerStable);
            isRunning = false;
            isJumping = false;
            isJumpingDetected = false;
            anim.SetBool("isRunning", isRunning);
            anim.SetBool("isJumping", isJumping);
        }

        private void OnEnable()
        {
            if (playerCamera)
                playerCamera.cameraTarget = transform;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, Vector3.down * jumpCheckDist);
        }

        public void Reset()
        {
            Debug.Log($"Resetting player #{PlayerIndex} ID:{PlayerId}");
            ready = false;
        }

        public void Respawn(float inSeconds = 0)
        {
            _respawnInSeconds = inSeconds;
        }

        private void CheckRespawn()
        {
            if (_respawnInSeconds >= 0)
            {
                _respawnInSeconds -= Runner.DeltaTime;

                if (_respawnInSeconds <= 0)
                {
                    SpawnPoint spawnpt = Runner.GetLevelManager().GetPlayerSpawnPoint(PlayerIndex);
                    if (spawnpt == null)
                    {
                        _respawnInSeconds = Runner.DeltaTime;
                        Debug.LogWarning($"No Spawn Point for player #{PlayerIndex} ID:{PlayerId} - trying again in {_respawnInSeconds} seconds");
                        return;
                    }

                    // Make sure we don't get in here again, even if we hit exactly zero
                    _respawnInSeconds = -1;

                    // Start the respawn timer and trigger the teleport in effect
                    respawnTimer = TickTimer.CreateFromSeconds(Runner, 1);

                    // Place the tank at its spawn point. This has to be done in FUN() because the transform gets reset otherwise
                    Transform spawn = spawnpt.transform;
                    transform.position = spawn.position;
                    transform.rotation = spawn.rotation;

                    // If the player was already here when we joined, it might already be active, in which case we don't want to trigger any spawn FX, so just leave it ACTIVE
                    if (stage != Stage.Active)
                        stage = Stage.TeleportIn;
                }
            }
        }

        public void OnStageChanged()
        {
            switch (stage)
            {
                case Stage.TeleportIn:
                    //_teleportIn.StartTeleport();
                    break;
                case Stage.Active:
                    //_teleportIn.EndTeleport();
                    break;
                case Stage.TeleportOut:
                    //SpawnTeleportOutFx();
                    break;
            }
        }

        private void SpawnTeleportOutFx()
        {
            //TankTeleportOutEffect teleout = LocalObjectPool.Acquire(_teleportOutPrefab, transform.position, transform.rotation, null);
            //teleout.StartTeleport(playerColor, turretRotation, hullRotation);
        }

        private void ResetPlayer()
        {
            stage = Stage.Active;
        }

        public void TeleportOut()
        {
            if (stage == Stage.Dead || stage == Stage.TeleportOut)
                return;

            if (Object.HasStateAuthority)
                stage = Stage.TeleportOut;
        }
    }
}