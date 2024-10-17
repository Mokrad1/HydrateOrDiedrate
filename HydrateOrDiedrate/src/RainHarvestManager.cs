﻿using System;
using System.Collections.Generic;
using HydrateOrDiedrate.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate;

public class RainHarvesterManager
{
    private ICoreServerAPI serverAPI;
    private Dictionary<BlockPos, RainHarvesterData> activeHarvesters;
    private Dictionary<BlockPos, RainHarvesterData> inactiveHarvesters;
    private long tickListenerId;
    private int globalTickCounter = 0;
    private bool enableParticleTicking;
    public RainHarvesterManager(ICoreServerAPI api)
    {
        serverAPI = api;
        activeHarvesters = new Dictionary<BlockPos, RainHarvesterData>();
        inactiveHarvesters = new Dictionary<BlockPos, RainHarvesterData>();

        var config = ModConfig.ReadConfig<Config.Config>(api, "HydrateOrDiedrateConfig.json");
        enableParticleTicking = config.EnableParticleTicking;
        if (config.EnableRainGathering)
        {
            tickListenerId = api.Event.RegisterGameTickListener(OnTick, 500);
            api.Event.RegisterGameTickListener(OnInactiveHarvesterCheck, 5000);
        }
    }

    public void RegisterHarvester(BlockPos position, RainHarvesterData data)
    {
        if (IsRainyAndOpenToSky(data))
        {
            activeHarvesters[position] = data;
        }
        else
        {
            inactiveHarvesters[position] = data;
        }
    }

    public void UnregisterHarvester(BlockPos position)
    {
        activeHarvesters.Remove(position);
        inactiveHarvesters.Remove(position);
    }

    public void OnBlockRemoved(BlockPos position)
    {
        UnregisterHarvester(position);
    }

    public void OnBlockUnloaded(BlockPos position)
    {
        UnregisterHarvester(position);
    }

    private void OnTick(float deltaTime)
    {
        globalTickCounter++;
        if (globalTickCounter > 40) globalTickCounter = 0;

        foreach (var entry in activeHarvesters)
        {
            BlockPos position = entry.Key;
            RainHarvesterData harvesterData = entry.Value;

            float rainIntensity = harvesterData.GetRainIntensity();
            harvesterData.UpdateCalculatedTickInterval(deltaTime, serverAPI.World.Calendar.SpeedOfTime,
                serverAPI.World.Calendar.CalendarSpeedMul, rainIntensity);

            int tickIntervalDifference = harvesterData.calculatedTickInterval - harvesterData.previousCalculatedTickInterval;
            harvesterData.adaptiveTickInterval += tickIntervalDifference;

            if (harvesterData.adaptiveTickInterval <= globalTickCounter &&
                harvesterData.previousCalculatedTickInterval > globalTickCounter)
            {
                harvesterData.adaptiveTickInterval = globalTickCounter + 2;
            }

            if (harvesterData.adaptiveTickInterval > 40)
            {
                harvesterData.adaptiveTickInterval -= 40;
            }

            if (globalTickCounter == harvesterData.adaptiveTickInterval)
            {
                int newAdaptiveTickInterval = harvesterData.adaptiveTickInterval + harvesterData.calculatedTickInterval;
                if (newAdaptiveTickInterval > 40)
                {
                    newAdaptiveTickInterval -= 40;
                }

                harvesterData.adaptiveTickInterval = newAdaptiveTickInterval;
                activeHarvesters[position] = harvesterData;
                harvesterData.OnHarvest(rainIntensity);
            }

            if (enableParticleTicking)
            {
                harvesterData.OnParticleTickUpdate(deltaTime);
            }
        }
    }

    private void OnInactiveHarvesterCheck(float deltaTime)
    {
        var toActivate = new List<BlockPos>();

        foreach (var entry in inactiveHarvesters)
        {
            BlockPos position = entry.Key;
            RainHarvesterData harvesterData = entry.Value;

            if (IsRainyAndOpenToSky(harvesterData))
            {
                toActivate.Add(position);
            }
        }

        foreach (var position in toActivate)
        {
            RainHarvesterData harvesterData = inactiveHarvesters[position];
            inactiveHarvesters.Remove(position);
            activeHarvesters[position] = harvesterData;
        }
        
    }

    private bool IsRainyAndOpenToSky(RainHarvesterData harvesterData)
    {
        return harvesterData.GetRainIntensity() > 0 && harvesterData.IsOpenToSky(harvesterData.BlockEntity.Pos);
    }

    private void LogDictionaryCounts()
    {
        serverAPI.Logger.Notification($"Active Harvesters: {activeHarvesters.Count}, Inactive Harvesters: {inactiveHarvesters.Count}");
    }
}