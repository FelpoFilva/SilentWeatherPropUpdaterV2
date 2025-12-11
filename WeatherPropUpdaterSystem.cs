using Game;
using Game.Common;
using Game.Simulation;
using Unity.Entities;
using Unity.Collections;
using Colossal.Logging;
using System;  // FIX: Add this for Math.Ceiling

namespace SilentWeatherPropUpdaterV2
{
    public partial class WeatherPropUpdaterSystem : GameSystemBase
    {
        private ClimateSystem _climateSystem;
        private ILog log;
        
        private enum SnowState { Stable, Accumulating, Melting }
        private SnowState _currentState = SnowState.Stable;
        private float _burstTimer = 0f;
        private float _updateTimer = 0f;
        private const float BURST_DURATION = 120f;
        private const float UPDATE_INTERVAL = 15f;

        protected override void OnCreate()
        {
            base.OnCreate();
            log = LogManager.GetLogger(nameof(WeatherPropUpdaterSystem));
            log.Info("System created (2-min burst mode)");
            
            _climateSystem = World.GetOrCreateSystemManaged<ClimateSystem>();
        }

        protected override void OnUpdate()
        {
            if (_climateSystem == null) return;

            // Determine current weather condition
            bool isPrecipitating = _climateSystem.precipitation > 0.1f;
            bool isCold = _climateSystem.temperature < -2f;
            bool isWarm = _climateSystem.temperature > 1f;

            // Determine target state
            SnowState targetState = SnowState.Stable;
            if (isPrecipitating && isCold) targetState = SnowState.Accumulating;
            else if (isWarm) targetState = SnowState.Melting;

            // If state changed, start a new burst
            if (targetState != _currentState && targetState != SnowState.Stable)
            {
                log.Info($"Snow state changed: {_currentState} → {targetState}, starting 2-min update burst");
                _currentState = targetState;
                _burstTimer = BURST_DURATION;
                _updateTimer = UPDATE_INTERVAL; // Force immediate update
            }
            // If stable, stop all updates
            else if (targetState == SnowState.Stable && _currentState != SnowState.Stable)
            {
                log.Info($"Snow state stabilized: {_currentState} → Stable, stopping updates");
                _currentState = SnowState.Stable;
                _burstTimer = 0f;
                _updateTimer = 0f;
            }

            // Only update during active burst
            if (_currentState != SnowState.Stable)
            {
                _burstTimer -= World.Time.DeltaTime;
                _updateTimer -= World.Time.DeltaTime;

                // Update every 15 seconds during burst
                if (_updateTimer <= 0f)
                {
                    _updateTimer = UPDATE_INTERVAL;
                    UpdateAllBuildings();
                    log.Info($"[{_currentState}] {Math.Ceiling(_burstTimer)}s remaining in burst");
                }

                // Stop after 2 minutes
                if (_burstTimer <= 0f)
                {
                    log.Info($"Burst complete, returning to stable state");
                    _currentState = SnowState.Stable;
                }
            }
        }

        private void UpdateAllBuildings()
        {
            var buildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.Building>());
            var buildings = buildingQuery.ToEntityArray(Allocator.TempJob);
            
            foreach (var buildingEntity in buildings)
            {
                EntityManager.AddComponent<Updated>(buildingEntity);
            }
            
            buildings.Dispose();
            log.Info($"Updated {buildings.Length} buildings");
        }
    }
}