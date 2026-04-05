using System.Collections;
using UnityEngine;

namespace VRArcaneArena.Game
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public int totalWaves = 5;
        public float timeBetweenWaves = 15f;
        public int baseEnemiesPerWave = 10;
        public int pointsPerKill = 10;
        public int pointsPerWave = 50;

        private int _currentWave = 0;
        private int _currentPoints = 0;
        private int _enemiesAlive = 0;
        private bool _gameStarted = false;
        private bool _gameOver = false;
        private bool _gameWon = false;
        private EnemySpawner _spawner;

        public int CurrentWave => _currentWave;
        public int CurrentPoints => _currentPoints;
        public bool HasGameStarted => _gameStarted;
        public bool IsGameOver => _gameOver;
        public bool IsGameWon => _gameWon;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _spawner = FindObjectOfType<EnemySpawner>();
        }

        public void StartGame()
        {
            if (_gameStarted || _gameOver || _gameWon)
            {
                return;
            }

            _gameStarted = true;
            StartCoroutine(StartNextWave());
        }

        private IEnumerator StartNextWave()
        {
            yield return new WaitForSeconds(_currentWave == 0 ? 5f : timeBetweenWaves);

            _currentWave++;
            if (_currentWave > totalWaves)
            {
                OnGameWon();
                yield break;
            }

            int enemyCount = baseEnemiesPerWave + (_currentWave - 1) * 5;
            _enemiesAlive = enemyCount;
            _spawner.SpawnWave(enemyCount);
            Debug.Log("Starting wave: " + _currentWave);
        }

        public void OnEnemyKilled()
        {
            if (!_gameStarted || _gameOver || _gameWon) return;

            _currentPoints += pointsPerKill;
            _enemiesAlive--;
            Debug.Log("Points: " + _currentPoints + " | Enemies remaining: " + _enemiesAlive);

            if (_enemiesAlive <= 0)
            {
                _currentPoints += pointsPerWave;
                StartCoroutine(StartNextWave());
            }
        }

        public void OnPlayerDied()
        {
            if (_gameOver) return;

            _gameOver = true;
            Debug.Log("GAME OVER — final score: " + _currentPoints);
        }

        public void OnGameWon()
        {
            _gameWon = true;
            Debug.Log("YOU WIN — final score: " + _currentPoints);
        }
    }
}
