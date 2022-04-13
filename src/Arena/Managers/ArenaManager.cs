﻿using Arena.Logging;
using Arena.Managers.Bases;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;

namespace Arena.Managers;

internal class ArenaManager : ListeningManagerBase
{
    public bool IsEventInProgress;

    public override IEnumerable<string> GetStatus() => new string[]
    {
        $"{ (IsListening ? "Watching stage events" : "Not watching stage events") }.",
        $"Arena event is { (IsEventInProgress ? "in progress" : "not in progress") }."
    };

    public void WatchStageEvents() => StartListening();

    protected override void StartListening()
    {
        TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterCharged;
        On.RoR2.Run.AdvanceStage += OnAdvanceStage;

        Log.Info($"Started watching stage events.");

        base.StartListening();
    }

    protected override void StopListening()
    {
        TeleporterInteraction.onTeleporterChargedGlobal -= OnTeleporterCharged;
        On.RoR2.Run.AdvanceStage -= OnAdvanceStage;

        Log.Info($"Stopped watching stage events.");

        base.StopListening();
    }

    private void OnTeleporterCharged(TeleporterInteraction tpi)
    {
        if (Store.Instance.Get<DeathManager>().IsOnePlayerAlive)
        {
            Log.Info("Only one player is alive. Not starting the Arena event.");
            return;
        }

        ChatMessage.Send("Good people of the Imperial City, welcome to the Arena!");

        Store.Instance.Get<ClockManager>().PauseClock();
        Store.Instance.Get<FriendlyFireManager>().EnableFriendlyFire();
        Store.Instance.Get<PortalManager>().DisableAllPortals();
        Store.Instance.Get<DeathManager>().WatchDeaths(OnChampionWon);

        IsEventInProgress = true;

        Log.Info("Arena event started.");
    }

    private void OnChampionWon(string championName)
    {
        Store.Instance.Get<PortalManager>().EnableAllPortals();

        ChatMessage.Send($"Good people, we have a winner! All hail the combatant, {championName}! Champion, leave the Arena now and rest! You've earned it!");
    }

    private void OnAdvanceStage(On.RoR2.Run.orig_AdvanceStage orig, Run self, SceneDef nextScene)
    {
        if (IsEventInProgress)
        {
            Store.Instance.Get<ClockManager>().ResumeClock();
            Store.Instance.Get<FriendlyFireManager>().DisableFriendlyFire();

            IsEventInProgress = false;

            Log.Info("Arena event ended.");
        }

        orig(self, nextScene);
    }
}
