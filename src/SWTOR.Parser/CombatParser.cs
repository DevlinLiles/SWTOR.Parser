using System;
using System.Linq;
using System.Collections.Generic;
using SWTOR.Parser.Domain;

namespace SWTOR.Parser
{
    public class CombatParser
    {
        public CombatLog Parse(List<LogEntry> log)
        {
            CombatLog cLog = ParseCombatLog(log);

            var allCombatLogs = cLog.Combats.SelectMany(m => m.Log);
            LogAnalyzer(cLog, allCombatLogs);

            foreach (var combat in cLog.Combats)
            {
                CharacterAnalyzer(combat, combat.Log);
                CombatAnalyzer(combat, combat.Log);
            }

            return cLog;
        }

        private void CharacterAnalyzer(CombatData combat, List<LogEntry> log)
        {
            var allSources = log.DistinctSources();
            var allTargets = log.DistinctTargets();
            var allCharacters = allSources.Union(allTargets).Distinct();

            foreach (var character in allCharacters)
            {
                var metrics = new CharacterData();
                combat.Characters.Add(character, metrics);

                CombatAnalyzer(metrics.AsSource, log.WithSource(character));
                CombatAnalyzer(metrics.AsTarget, log.WithTarget(character));
            }
        }

        private void CombatAnalyzer(ICombatMetrics data, IEnumerable<LogEntry> log)
        {
            if (log.Count() == 0) return;

            LogAnalyzer(data, log);
            data.Interval = Convert.ToInt32((log.EndTime() - log.StartTime()).TotalSeconds);
            data.AverageDamagePerSecond = (double)data.TotalDamage / data.Interval;
            data.AverageHealingPerSecond = (double)data.TotalHealing / data.Interval;
            data.AverageThreatPerSecond = (double)data.TotalThreat / data.Interval;
        }

        private void LogAnalyzer(ILogMetrics data, IEnumerable<LogEntry> log)
        {
            data.TotalDamage = log.DamageEffects().Sum(m => m.result.amount);
            data.TotalHealing = log.HealingEffects().Sum(m => m.result.amount);
            data.TotalThreat = log.ThreatEffects().Sum(m => m.threat);
            data.CountOfParry = log.ParryEffects().Count();
            data.CountOfDeflect = log.DeflectEffects().Count();

            var abilityNames = log.Where(m => m.ability.name != "").Select(m => m.ability.name).Distinct();
            foreach (var name in abilityNames)
            {
                var count = new AbilityCount();
                data.AbilityCounts.Add(count);
                count.Name = name;
                count.Number = log.First(m => m.ability.name == name).ability.number;
                count.Count = log.Count(m => m.ability.name == name);
            }
            data.AbilityCounts = data.AbilityCounts.OrderByDescending(m => m.Count).ThenBy(m => m.Name).ToList();
        }

        private CombatLog ParseCombatLog(List<LogEntry> log)
        {
            var cLog = new CombatLog();
            CombatData currentCombat = null;

            foreach (LogEntry logEntry in log)
            {
                if (logEntry.effect.name == "EnterCombat")
                    currentCombat = new CombatData();

                if (currentCombat != null)
                    currentCombat.Log.Add(logEntry);

                if (logEntry.effect.name == "ExitCombat")
                {
                    cLog.Combats.Add(currentCombat);
                    currentCombat = null;
                }
            }

            if (currentCombat != null) // we have a partial combat
                cLog.Combats.Add(currentCombat);
            return cLog;
        }
    }
}