using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    #region Headers and Fields

    [Header("Camera Settings")]
    public Camera mainCamera;

    [Header("Player Settings")]
    public Transform player;
    public Sprite playerSprite;
    public Vector2 initialPlayerPosition = new Vector2(2f, 2f);
    public Vector2 playerScale = Vector2.one;
    public float playerSpeed = 5f;
    public bool isPlayerActive = true;
    public float maxPlayerHealth = 100f;
    public float playerHealth = 100f;
    private float lastPlayerDamageTime;
    private Vector2 lastPlayerMovementDirection;
    public float collectionRadius = 2f;

    [Header("Player Settings/Weapons")]
    public GameObject projectilePrefab;
    [SerializeField]
    private List<WeaponData> weapons = new List<WeaponData>();

    [Header("Enemies")]
    public GameObject enemyPrefab;
    [SerializeField]
    private List<EnemyData> enemyTypes = new List<EnemyData>();

    [Header("Spawner Events")]
    [SerializeField]
    private List<SpawnerEvent> spawnerEvents = new List<SpawnerEvent>();

    [Header("Map Settings")]
    public GameObject infiniteGrids;
    public Vector2 baseSize = new Vector2(18f, 10f);

    [Header("UI Settings")]
    public TextMeshProUGUI timerText;
    public UnityEngine.UI.Image healthBarFill;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI unitText;
    public TextMeshProUGUI coordinateText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    private float gameTime;
    private int totalKills;
    private int totalUnits;
    private bool gameEnded;

    [Header("Damage Text Settings")]
    public GameObject damageTextPrefab; // Prefab with TextMeshPro component
    public float damageTextDuration = 0.5f; // Duration of damage text
    public float damageTextSpeed = 2f; // Speed of floating upward
    public Vector2 damageTextOffset = new Vector2(0f, 0.5f); // Offset above object
    public float minFontSize = 2f; // Minimum font size for damage text
    public float maxFontSize = 4f; // Maximum font size for damage text
    public Color criticalHitColor = Color.red; // Color for critical hits
    public Color standardHitColor = Color.white; // Color for standard hits

    [Header("Collectable Settings")]
    [SerializeField]
    private List<CollectableData> collectables = new List<CollectableData>();
    private List<CollectableInstance> activeCollectables = new List<CollectableInstance>();
    private Dictionary<string, int> playerCurrency = new Dictionary<string, int>();

    #endregion

    #region Nested Classes and Enums

    [System.Serializable]
    public class WeaponData
    {
        public bool isActive = true;
        public string name = "Weapon";
        public float speed = 5f;
        public float rate = 2f;
        public Vector2 scale = Vector2.one;
        public float damage = 10f;
        public WeaponDirection direction = WeaponDirection.LastMovedDirection;

        [Header("Critical Hit Settings")]
        public float minCriticalDamage = 15f; // Minimum critical hit damage
        public float maxCriticalDamage = 25f; // Maximum critical hit damage
        public float criticalChance = 0.1f; // 10% chance of critical hit

        [System.NonSerialized]
        public float nextFireTime;
    }

    public enum WeaponDirection
    {
        LastMovedDirection,
        NearestEnemy,
        CirclingPlayer,
        FourPoint,
        EightPoint
    }

    private class ProjectileInstance
    {
        public GameObject projectileObject;
        public WeaponData weaponData;
        public Vector2 initialDirection;
        public float angle;
        public float lifetime;

        public ProjectileInstance(GameObject projectileObject, WeaponData weaponData, Vector2 initialDirection)
        {
            this.projectileObject = projectileObject;
            this.weaponData = weaponData;
            this.initialDirection = initialDirection;
            this.angle = 0f;
            this.lifetime = 0f;
        }
    }
    private List<ProjectileInstance> activeProjectiles = new List<ProjectileInstance>();

    [System.Serializable]
    public class EnemyData
    {
        public bool isActive = true;
        public string name = "Enemy";
        public Sprite sprite;
        public Vector2 size = Vector2.one;
        public float speed = 3f;
        public float health = 50f;
        public float damage = 10f;
        public string collectableName = "";
        public float dropRate = 100f;
    }

    private class EnemyInstance
    {
        public GameObject enemyObject;
        public EnemyData data;
        public Vector2 lastPosition;
        public float stationaryTime;
        public float lastDamageTime;
        public bool isInContact;
        public bool isWallCollisionDisabled;
        public float collisionDisableTimer;

        public EnemyInstance(GameObject enemyObject, EnemyData data)
        {
            this.enemyObject = enemyObject;
            this.data = data;
            lastPosition = enemyObject.transform.position;
            stationaryTime = 0f;
            lastDamageTime = -1f;
            isInContact = false;
            isWallCollisionDisabled = false;
            collisionDisableTimer = 0f;
        }
    }
    private List<EnemyInstance> activeEnemies = new List<EnemyInstance>();
    private List<string> activeEnemyNames = new List<string>();

    [System.Serializable]
    public class SpawnerEvent
    {
        public string spawnType;
        public int spawnAmount = 1;
        public float interval = 5f;
        public float startTime = 0f;
        public float endTime = 60f;
        public bool randomSpawn = false;
        public bool swarm = false;
        public SpawnDirection spawnPoint = SpawnDirection.Up;

        [System.NonSerialized]
        public float nextSpawnTime;
    }

    public enum SpawnDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [System.Serializable]
    public class CollectableData
    {
        public GameObject prefab;
        public string name = "Collectable";
        public Sprite sprite;
        public int unitValue = 1;
        public Vector2 scale = new Vector2(0.2f, 0.2f);
        public Color color = Color.white;
        public bool glowEnabled = false;
        public Color glowColor = Color.yellow;
        public float glowRadiusMin = 1.2f;
        public float glowRadiusMax = 1.5f;
        public float glowBrightnessMin = 0.3f;
        public float glowBrightnessMax = 0.8f;
        public float glowSpeed = 2f;
    }

    private class CollectableInstance
    {
        public GameObject collectableObject;
        public CollectableData data;
        public GameObject glowObject;
        public SpriteRenderer glowRenderer;
        public float glowTime;

        public CollectableInstance(GameObject collectableObject, CollectableData data, GameObject glowObject, SpriteRenderer glowRenderer)
        {
            this.collectableObject = collectableObject;
            this.data = data;
            this.glowObject = glowObject;
            this.glowRenderer = glowRenderer;
            this.glowTime = Random.Range(0f, 2f * Mathf.PI);
        }
    }

    #endregion

    #region Private Fields

    private Vector2Int lastPlayerChunk;
    private List<Transform> gridCopies = new List<Transform>();
    private float initialHealthBarWidth;

    #endregion

    #region Unity Methods

    void Start()
    {
        Screen.SetResolution(1920, 1080, true);
        if (player == null)
        {
            Debug.LogError("Player Transform not assigned in GameManager!");
            return;
        }

        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (playerSprite != null)
            {
                sr.sprite = playerSprite;
            }
            sr.sortingOrder = 10;
        }
        player.position = initialPlayerPosition;
        player.localScale = playerScale;
        player.gameObject.layer = LayerMask.NameToLayer("Player");

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy Prefab not assigned in GameManager!");
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab not assigned in GameManager!");
            return;
        }

        if (damageTextPrefab == null)
        {
            Debug.LogError("Damage Text Prefab not assigned in GameManager!");
            return;
        }

        for (int i = 0; i < collectables.Count; i++)
        {
            if (collectables[i].prefab == null)
            {
                Debug.LogError($"Collectable {collectables[i].name} prefab not assigned in GameManager!");
            }
            if (!playerCurrency.ContainsKey(collectables[i].name))
            {
                playerCurrency.Add(collectables[i].name, 0);
            }
        }

        for (int i = 0; i < infiniteGrids.transform.childCount; i++)
        {
            Transform grid = infiniteGrids.transform.GetChild(i);
            gridCopies.Add(grid);
            SpriteRenderer gridSr = grid.GetComponent<SpriteRenderer>();
            if (gridSr != null)
            {
                gridSr.sortingOrder = 0;
            }
        }

        lastPlayerChunk = GetPlayerChunk();
        gameTime = 0f;
        lastPlayerDamageTime = -1f;
        lastPlayerMovementDirection = Vector2.right;

        playerHealth = maxPlayerHealth;
        if (healthBarFill != null)
        {
            initialHealthBarWidth = healthBarFill.rectTransform.sizeDelta.x;
            UpdateHealthBar();
        }
        else
        {
            Debug.LogError("HealthBarFill Image not assigned in GameManager!");
        }

        activeEnemyNames.Clear();
        for (int i = 0; i < enemyTypes.Count; i++)
        {
            var enemyType = enemyTypes[i];
            if (enemyType.isActive)
            {
                activeEnemyNames.Add(enemyType.name);
            }
            Debug.Log($"Enemy Type {i}: {enemyType.name} (Active: {enemyType.isActive})");
        }

        Debug.Log("Available Enemy Names for Spawner Events: " + string.Join(", ", activeEnemyNames));

        foreach (var spawnerEvent in spawnerEvents)
        {
            spawnerEvent.nextSpawnTime = spawnerEvent.startTime;
            if (!activeEnemyNames.Contains(spawnerEvent.spawnType))
            {
                Debug.LogWarning($"Spawner Event has invalid Spawn Type '{spawnerEvent.spawnType}'. Available types: {string.Join(", ", activeEnemyNames)}");
            }
        }

        foreach (var weapon in weapons)
        {
            weapon.nextFireTime = 0f;
        }

        totalKills = 0;
        totalUnits = 0;
        gameEnded = false;
        UpdateKillsDisplay();
        UpdateUnitsDisplay();
        UpdateCoordinatesDisplay();

        if (unitText != null && coordinateText != null && killsText != null && timerText != null)
        {
            unitText.font = killsText.font;
            unitText.color = killsText.color;
            unitText.alignment = killsText.alignment;
            unitText.fontStyle = killsText.fontStyle;
            unitText.fontMaterial = killsText.fontMaterial;

            coordinateText.font = killsText.font;
            coordinateText.color = killsText.color;
            coordinateText.alignment = killsText.alignment;
            coordinateText.fontStyle = killsText.fontStyle;
            coordinateText.fontMaterial = killsText.fontMaterial;
        }
        else
        {
            if (unitText == null) Debug.LogError("Unit Text not assigned in GameManager!");
            if (coordinateText == null) Debug.LogError("Coordinate Text not assigned in GameManager!");
            if (killsText == null) Debug.LogError("Kills Text not assigned in GameManager!");
            if (timerText == null) Debug.LogError("Timer Text not assigned in GameManager!");
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Game Over Panel not assigned in GameManager!");
        }

        if (finalScoreText != null)
        {
            finalScoreText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Final Score Text not assigned in GameManager!");
        }
    }

    void FixedUpdate()
    {
        if (isPlayerActive && !gameEnded)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector2 movement = new Vector2(horizontal, vertical).normalized * playerSpeed * Time.fixedDeltaTime;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.MovePosition(rb.position + movement);
            }
            if (movement != Vector2.zero)
            {
                lastPlayerMovementDirection = movement.normalized;
            }

            foreach (var weapon in weapons)
            {
                if (weapon.isActive && gameTime >= weapon.nextFireTime)
                {
                    FireWeapon(weapon);
                    weapon.nextFireTime = gameTime + (1f / weapon.rate);
                }
            }

            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                var projectile = activeProjectiles[i];
                if (projectile.projectileObject == null)
                {
                    activeProjectiles.RemoveAt(i);
                    continue;
                }

                projectile.lifetime += Time.fixedDeltaTime;
                if (projectile.lifetime > 5f && projectile.weaponData.direction != WeaponDirection.CirclingPlayer)
                {
                    Destroy(projectile.projectileObject);
                    activeProjectiles.RemoveAt(i);
                    continue;
                }

                if (projectile.weaponData.direction == WeaponDirection.CirclingPlayer)
                {
                    projectile.angle += projectile.weaponData.speed * Time.fixedDeltaTime * 2f;
                    Vector2 offset = new Vector2(Mathf.Cos(projectile.angle), Mathf.Sin(projectile.angle)) * 2f;
                    projectile.projectileObject.transform.position = (Vector2)player.position + offset;
                }
                else
                {
                    Collider2D wallHit = Physics2D.OverlapCircle(projectile.projectileObject.transform.position, 0.2f, LayerMask.GetMask("Walls"));
                    if (wallHit != null)
                    {
                        Destroy(projectile.projectileObject);
                        activeProjectiles.RemoveAt(i);
                        continue;
                    }
                }

                for (int j = activeEnemies.Count - 1; j >= 0; j--)
                {
                    var enemy = activeEnemies[j];
                    float distance = Vector2.Distance(projectile.projectileObject.transform.position, enemy.enemyObject.transform.position);
                    if (distance < 0.5f)
                    {
                        bool isCritical = Random.value < projectile.weaponData.criticalChance;
                        float damage = isCritical
                            ? Random.Range(projectile.weaponData.minCriticalDamage, projectile.weaponData.maxCriticalDamage)
                            : projectile.weaponData.damage;
                        enemy.data.health -= damage;
                        SpawnDamageText(enemy.enemyObject.transform.position, damage, isCritical);
                        Debug.Log($"{enemy.data.name} took {damage} damage. Health: {enemy.data.health}");
                        if (projectile.weaponData.direction != WeaponDirection.CirclingPlayer)
                        {
                            Destroy(projectile.projectileObject);
                            activeProjectiles.RemoveAt(i);
                        }

                        if (enemy.data.health <= 0)
                        {
                            float dropChance = Random.Range(0f, 100f);
                            if (dropChance <= enemy.data.dropRate && !string.IsNullOrEmpty(enemy.data.collectableName))
                            {
                                CollectableData collectableData = collectables.Find(c => c.name == enemy.data.collectableName);
                                if (collectableData != null)
                                {
                                    Vector2 spawnPosition = enemy.enemyObject.transform.position + (Vector3)(Random.insideUnitCircle * 0.5f);
                                    GameObject collectable = Instantiate(collectableData.prefab, spawnPosition, Quaternion.identity);
                                    collectable.layer = LayerMask.NameToLayer("Collectable");
                                    SpriteRenderer sr = collectable.GetComponent<SpriteRenderer>();
                                    if (sr != null)
                                    {
                                        sr.sprite = collectableData.sprite;
                                        sr.color = collectableData.color;
                                        sr.sortingOrder = 5;
                                    }
                                    collectable.transform.localScale = collectableData.scale;

                                    GameObject glowObject = null;
                                    SpriteRenderer glowRenderer = null;
                                    if (collectableData.glowEnabled)
                                    {
                                        glowObject = new GameObject("Glow");
                                        glowObject.transform.SetParent(collectable.transform);
                                        glowObject.transform.localPosition = Vector3.zero;
                                        glowRenderer = glowObject.AddComponent<SpriteRenderer>();
                                        glowRenderer.sprite = collectableData.sprite;
                                        glowRenderer.sortingLayerName = sr.sortingLayerName;
                                        glowRenderer.sortingOrder = sr.sortingOrder - 1;
                                        glowRenderer.color = collectableData.glowColor;
                                    }

                                    activeCollectables.Add(new CollectableInstance(collectable, collectableData, glowObject, glowRenderer));
                                }
                                else
                                {
                                    Debug.LogWarning($"Collectable '{enemy.data.collectableName}' not found for enemy '{enemy.data.name}'!");
                                }
                            }
                            totalKills += 1;
                            UpdateKillsDisplay();
                            Destroy(enemy.enemyObject);
                            activeEnemies.RemoveAt(j);
                            Debug.Log($"{enemy.data.name} defeated! Kills: {totalKills}");
                        }
                        break;
                    }
                }
            }

            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemyInstance = activeEnemies[i];
                if (enemyInstance.enemyObject == null)
                {
                    activeEnemies.RemoveAt(i);
                    continue;
                }

                Vector2 currentEnemyPosition = enemyInstance.enemyObject.transform.position;
                float distanceMoved = Vector2.Distance(currentEnemyPosition, enemyInstance.lastPosition);
                if (distanceMoved < 0.05f)
                {
                    enemyInstance.stationaryTime += Time.fixedDeltaTime;
                }
                else
                {
                    enemyInstance.stationaryTime = 0f;
                }
                enemyInstance.lastPosition = currentEnemyPosition;

                if (enemyInstance.isWallCollisionDisabled)
                {
                    enemyInstance.collisionDisableTimer += Time.fixedDeltaTime;
                    if (enemyInstance.collisionDisableTimer >= 0.5f)
                    {
                        enemyInstance.enemyObject.layer = LayerMask.NameToLayer("Enemy");
                        enemyInstance.isWallCollisionDisabled = false;
                        enemyInstance.collisionDisableTimer = 0f;
                        enemyInstance.stationaryTime = 0f;
                        Debug.Log($"{enemyInstance.data.name} re-enabled wall collision.");
                    }
                }
                else if (enemyInstance.stationaryTime >= 1.5f)
                {
                    enemyInstance.enemyObject.layer = LayerMask.NameToLayer("EnemyNoWallCollision");
                    enemyInstance.isWallCollisionDisabled = true;
                    enemyInstance.collisionDisableTimer = 0f;
                    Debug.Log($"{enemyInstance.data.name} disabled wall collision to get unstuck.");
                }

                Vector2 directionToPlayer = (Vector2)player.position - (Vector2)enemyInstance.enemyObject.transform.position;
                Vector2 enemyMovement = directionToPlayer.normalized * enemyInstance.data.speed * Time.fixedDeltaTime;

                Rigidbody2D enemyRb = enemyInstance.enemyObject.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    enemyRb.MovePosition(enemyRb.position + enemyMovement);
                }

                float distance = Vector2.Distance(player.position, enemyInstance.enemyObject.transform.position);
                float collisionDistance = 1f;

                bool currentlyInContact = distance < collisionDistance;

                if (currentlyInContact && !enemyInstance.isInContact)
                {
                    playerHealth -= enemyInstance.data.damage;
                    SpawnDamageText(player.position, enemyInstance.data.damage, false);
                    enemyInstance.lastDamageTime = Time.time;
                    enemyInstance.isInContact = true;
                    Debug.Log($"Player Health: {playerHealth} (Initial contact damage by {enemyInstance.data.name})");
                    UpdateHealthBar();
                    if (playerHealth <= 0f)
                    {
                        isPlayerActive = false;
                        gameEnded = true;
                        Debug.Log("Player defeated!");
                        StartCoroutine(DisplayFinalKills());
                    }
                }
                else if (currentlyInContact && enemyInstance.isInContact && Time.time > enemyInstance.lastDamageTime + 1f)
                {
                    playerHealth -= enemyInstance.data.damage;
                    SpawnDamageText(player.position, enemyInstance.data.damage, false);
                    enemyInstance.lastDamageTime = Time.time;
                    Debug.Log($"Player Health: {playerHealth} (Continuous contact damage by {enemyInstance.data.name})");
                    UpdateHealthBar();
                    if (playerHealth <= 0f)
                    {
                        isPlayerActive = false;
                        gameEnded = true;
                        Debug.Log("Player defeated!");
                        StartCoroutine(DisplayFinalKills());
                    }
                }
                else if (!currentlyInContact && enemyInstance.isInContact)
                {
                    enemyInstance.isInContact = false;
                }
            }

            Vector2Int currentChunk = GetPlayerChunk();
            if (currentChunk != lastPlayerChunk)
            {
                UpdateGrids(currentChunk);
                lastPlayerChunk = currentChunk;
            }
        }
    }

    void Update()
    {
        if (isPlayerActive && !gameEnded)
        {
            if (Input.GetKey(KeyCode.G))
            {
                gameTime += Time.deltaTime * 15f;
            }
            else
            {
                gameTime += Time.deltaTime;
            }

            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            if (timerText != null)
            {
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                Debug.Log("Timer updated: " + timerText.text);
            }

            if (gameTime >= 1200f)
            {
                gameEnded = true;
                isPlayerActive = false;
                Debug.Log("Level ended at 20:00!");
                StartCoroutine(DisplayFinalKills());
            }

            foreach (var spawnerEvent in spawnerEvents)
            {
                if (gameTime >= spawnerEvent.startTime && gameTime <= spawnerEvent.endTime)
                {
                    if (gameTime >= spawnerEvent.nextSpawnTime)
                    {
                        SpawnEnemies(spawnerEvent);
                        spawnerEvent.nextSpawnTime = gameTime + spawnerEvent.interval;
                    }
                }
            }

            for (int i = activeCollectables.Count - 1; i >= 0; i--)
            {
                CollectableInstance collectableInstance = activeCollectables[i];
                GameObject collectable = collectableInstance.collectableObject;
                if (collectable == null)
                {
                    activeCollectables.RemoveAt(i);
                    continue;
                }

                if (collectableInstance.data.glowEnabled && collectableInstance.glowRenderer != null)
                {
                    collectableInstance.glowTime += Time.deltaTime * collectableInstance.data.glowSpeed;
                    float t = (Mathf.Sin(collectableInstance.glowTime) + 1f) / 2f;

                    float scale = Mathf.Lerp(collectableInstance.data.glowRadiusMin, collectableInstance.data.glowRadiusMax, t);
                    collectableInstance.glowObject.transform.localScale = collectableInstance.data.scale * scale;

                    float alpha = Mathf.Lerp(collectableInstance.data.glowBrightnessMin, collectableInstance.data.glowBrightnessMax, t);
                    Color glowColor = collectableInstance.data.glowColor;
                    collectableInstance.glowRenderer.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
                }

                Vector2 position = collectable.transform.position;
                float distance = Vector2.Distance(position, player.position);
                if (distance <= collectionRadius)
                {
                    Vector2 direction = (player.position - (Vector3)position).normalized;
                    position += direction * 5f * Time.deltaTime;
                    collectable.transform.position = position;
                    if (distance <= 0.1f)
                    {
                        if (playerCurrency.ContainsKey(collectableInstance.data.name))
                        {
                            playerCurrency[collectableInstance.data.name] += collectableInstance.data.unitValue;
                        }
                        else
                        {
                            playerCurrency.Add(collectableInstance.data.name, collectableInstance.data.unitValue);
                        }
                        totalUnits += collectableInstance.data.unitValue;
                        UpdateUnitsDisplay();
                        Destroy(collectable);
                        activeCollectables.RemoveAt(i);
                        Debug.Log($"Collected {collectableInstance.data.unitValue} units of {collectableInstance.data.name}. Total: {playerCurrency[collectableInstance.data.name]}");
                    }
                }
            }
        }
    }

    void LateUpdate()
    {
        if (mainCamera != null && player != null)
        {
            mainCamera.transform.position = new Vector3(player.position.x, player.position.y, -10f);
            UpdateCoordinatesDisplay();
        }
    }

    #endregion

    #region Helper Methods

    void FireWeapon(WeaponData weapon)
    {
        List<Vector2> directions = new List<Vector2>();

        switch (weapon.direction)
        {
            case WeaponDirection.LastMovedDirection:
                directions.Add(lastPlayerMovementDirection);
                break;
            case WeaponDirection.NearestEnemy:
                EnemyInstance nearestEnemy = null;
                float minDistance = float.MaxValue;
                foreach (var enemy in activeEnemies)
                {
                    float distance = Vector2.Distance(player.position, enemy.enemyObject.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestEnemy = enemy;
                    }
                }
                if (nearestEnemy != null)
                {
                    Vector2 directionToEnemy = ((Vector2)nearestEnemy.enemyObject.transform.position - (Vector2)player.position).normalized;
                    directions.Add(directionToEnemy);
                }
                break;
            case WeaponDirection.CirclingPlayer:
                Vector2 initialDirection = Vector2.right;
                directions.Add(initialDirection);
                break;
            case WeaponDirection.FourPoint:
                directions.Add(Vector2.up);
                directions.Add(Vector2.down);
                directions.Add(Vector2.left);
                directions.Add(Vector2.right);
                break;
            case WeaponDirection.EightPoint:
                directions.Add(Vector2.up);
                directions.Add(Vector2.down);
                directions.Add(Vector2.left);
                directions.Add(Vector2.right);
                directions.Add(new Vector2(1, 1).normalized);
                directions.Add(new Vector2(1, -1).normalized);
                directions.Add(new Vector2(-1, 1).normalized);
                directions.Add(new Vector2(-1, -1).normalized);
                break;
        }

        foreach (var direction in directions)
        {
            GameObject projectile = Instantiate(projectilePrefab, player.position, Quaternion.identity);
            projectile.layer = LayerMask.NameToLayer("Projectile");
            projectile.transform.localScale = weapon.scale;

            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (weapon.direction != WeaponDirection.CirclingPlayer)
                {
                    rb.linearVelocity = direction * weapon.speed;
                }
            }

            activeProjectiles.Add(new ProjectileInstance(projectile, weapon, direction));
        }
    }

    void SpawnEnemies(SpawnerEvent spawnerEvent)
    {
        EnemyData enemyData = enemyTypes.Find(e => e.name == spawnerEvent.spawnType && e.isActive);
        if (enemyData == null)
        {
            Debug.LogWarning($"No active enemy type found with name {spawnerEvent.spawnType} for spawning!");
            return;
        }

        Vector2 playerPos = player.position;
        Vector2 spawnOffset;
        switch (spawnerEvent.spawnPoint)
        {
            case SpawnDirection.Up:
                spawnOffset = new Vector2(0, 20);
                break;
            case SpawnDirection.Down:
                spawnOffset = new Vector2(0, -20);
                break;
            case SpawnDirection.Left:
                spawnOffset = new Vector2(-20, 0);
                break;
            case SpawnDirection.Right:
                spawnOffset = new Vector2(20, 0);
                break;
            default:
                spawnOffset = Vector2.zero;
                break;
        }

        Vector2 baseSpawnPosition = playerPos + spawnOffset;

        for (int i = 0; i < spawnerEvent.spawnAmount; i++)
        {
            Vector2 spawnPosition = baseSpawnPosition;

            if (spawnerEvent.randomSpawn)
            {
                spawnPosition += Random.insideUnitCircle * 5f;
            }
            else if (spawnerEvent.swarm)
            {
                spawnPosition += Random.insideUnitCircle * 1f;
            }

            GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            newEnemy.layer = LayerMask.NameToLayer("Enemy");

            SpriteRenderer sr = newEnemy.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = enemyData.sprite;
                sr.sortingOrder = 10;
            }
            newEnemy.transform.localScale = enemyData.size;

            activeEnemies.Add(new EnemyInstance(newEnemy, enemyData));
        }

        Debug.Log($"Spawned {spawnerEvent.spawnAmount} {spawnerEvent.spawnType}(s) at {spawnerEvent.spawnPoint}");
    }

    void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercentage = playerHealth / maxPlayerHealth;
            healthPercentage = Mathf.Clamp01(healthPercentage);
            Vector2 newSize = healthBarFill.rectTransform.sizeDelta;
            newSize.x = initialHealthBarWidth * healthPercentage;
            healthBarFill.rectTransform.sizeDelta = newSize;
        }
    }

    void UpdateKillsDisplay()
    {
        if (killsText != null)
        {
            killsText.text = $"KILLS: {totalKills}";
        }
        else
        {
            Debug.LogError("Kills Text not assigned in GameManager!");
        }
    }

    void UpdateUnitsDisplay()
    {
        if (unitText != null)
        {
            unitText.text = $"UNITS: {totalUnits}";
        }
    }

    void UpdateCoordinatesDisplay()
    {
        if (coordinateText != null && player != null)
        {
            Vector2 playerPos = player.position;
            coordinateText.text = $"{Mathf.RoundToInt(playerPos.x)}, {Mathf.RoundToInt(playerPos.y)}";
        }
    }

    void SpawnDamageText(Vector3 position, float damage, bool isCritical)
    {
        GameObject textObj = Instantiate(damageTextPrefab, position + (Vector3)damageTextOffset, Quaternion.identity);
        TextMeshPro text = textObj.GetComponent<TextMeshPro>();
        if (text != null)
        {
            text.text = Mathf.RoundToInt(damage).ToString();
            text.color = isCritical ? criticalHitColor : standardHitColor;
            text.fontSize = Random.Range(minFontSize, maxFontSize);
            text.GetComponent<Renderer>().sortingLayerName = "Default";
            text.GetComponent<Renderer>().sortingOrder = 20; // Above player and enemies
        }
        StartCoroutine(AnimateDamageText(textObj));
    }

    IEnumerator AnimateDamageText(GameObject textObj)
    {
        float elapsed = 0f;
        Vector3 startPos = textObj.transform.position;

        while (elapsed < damageTextDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / damageTextDuration;
            textObj.transform.position = startPos + new Vector3(0f, t * damageTextSpeed, 0f);
            TextMeshPro text = textObj.GetComponent<TextMeshPro>();
            if (text != null)
            {
                text.color = new Color(text.color.r, text.color.g, text.color.b, 1f - t); // Fade out
            }
            yield return null;
        }

        Destroy(textObj);
    }

    IEnumerator DisplayFinalKills()
    {
        if (gameOverPanel != null && finalScoreText != null)
        {
            gameOverPanel.SetActive(true);

            finalScoreText.text = $"TOTAL KILLS: {totalKills}";
            finalScoreText.gameObject.SetActive(true);

            float flashDuration = 2f;
            float flashInterval = 0.5f;
            int flashCount = Mathf.FloorToInt(flashDuration / flashInterval);

            for (int i = 0; i < flashCount; i++)
            {
                finalScoreText.enabled = false;
                yield return new WaitForSeconds(flashInterval / 2f);
                finalScoreText.enabled = true;
                yield return new WaitForSeconds(flashInterval / 2f);
            }

            finalScoreText.enabled = true;
        }
    }

    Vector2Int GetPlayerChunk()
    {
        Vector2 pos = player.position;
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / baseSize.x),
            Mathf.FloorToInt(pos.y / baseSize.y)
        );
    }

    void UpdateGrids(Vector2Int playerChunk)
    {
        Vector2 chunkCenter = new Vector2(
            playerChunk.x * baseSize.x,
            playerChunk.y * baseSize.y
        );
        foreach (Transform grid in gridCopies)
        {
            Vector3 gridPos = grid.position;
            Vector2 offset = gridPos - (Vector3)chunkCenter;

            if (offset.x < -baseSize.x * 1.5f)
                gridPos.x += baseSize.x * 3f;
            else if (offset.x > baseSize.x * 1.5f)
                gridPos.x -= baseSize.x * 3f;
            if (offset.y < -baseSize.y * 1.5f)
                gridPos.y += baseSize.y * 3f;
            else if (offset.y > baseSize.y * 1.5f)
                gridPos.y -= baseSize.y * 3f;

            grid.position = gridPos;
        }
    }

    #endregion
}