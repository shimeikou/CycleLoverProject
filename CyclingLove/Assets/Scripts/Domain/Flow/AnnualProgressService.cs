using System.Collections.Generic;
using System.Linq;
using System;
using CyclingLove.State;

namespace CyclingLove.Calendar
{
    public sealed class AnnualProgressResult
    {
        public string DefinitionId, ResultFlag;
        public bool Succeeded, Applied;
    }

    public sealed class AnnualProgressService
    {
        private readonly AnnualCalendarDefinition definition;
        private readonly AnnualCalendarService calendar;

        public AnnualProgressService(AnnualCalendarDefinition source)
        {
            definition = (source ?? throw new ArgumentNullException(nameof(source))).Snapshot();
            calendar = new AnnualCalendarService(definition);
        }

        public AnnualProgressResult ResolveSeasonGoal(GamePlayerData data, string seasonId)
        {
            RequireState(data);
            data.SeasonGoalResults ??= new System.Collections.Generic.List<SeasonGoalRecord>();
            var goal = definition.SeasonGoals.SingleOrDefault(value => value.SeasonId == seasonId) ??
                       throw new InvalidOperationException("Season goal is missing: " + seasonId);
            var existing = data.SeasonGoalResults.FirstOrDefault(value => value.GoalId == goal.Id);
            if (existing != null)
                return new AnnualProgressResult { DefinitionId = existing.GoalId, ResultFlag = existing.ResultFlag, Succeeded = existing.Succeeded, Applied = false };

            var currentDate = CurrentDate(data);
            if (currentDate < goal.Deadline)
                throw new InvalidOperationException("Season goal cannot be resolved before its deadline");
            var evaluated = calendar.EvaluateSeasonGoal(data, seasonId);
            data.SeasonGoalResults.Add(new SeasonGoalRecord
            {
                GoalId = evaluated.GoalId,
                SeasonId = evaluated.SeasonId,
                ResultFlag = evaluated.ResultFlag,
                Succeeded = evaluated.Succeeded,
                EvaluatedYear = data.Year,
                EvaluatedMonth = data.Month,
                EvaluatedDay = data.Day
            });
            AddFlag(data, evaluated.ResultFlag);
            data.Revision++;
            return new AnnualProgressResult { DefinitionId = evaluated.GoalId, ResultFlag = evaluated.ResultFlag, Succeeded = evaluated.Succeeded, Applied = true };
        }

        public AnnualProgressResult ResolveEnding(GamePlayerData data)
        {
            RequireState(data);
            if (data.AnnualEndingResult?.IsResolved == true)
                return new AnnualProgressResult
                {
                    DefinitionId = data.AnnualEndingResult.EndingId,
                    ResultFlag = data.AnnualEndingResult.ResultFlag,
                    Succeeded = data.AnnualEndingResult.Succeeded,
                    Applied = false
                };
            if (CurrentDate(data) < definition.EndDate)
                throw new InvalidOperationException("Annual ending cannot be resolved before the final day");
            var ending = calendar.ResolveEnding(data);
            data.AnnualEndingResult = new AnnualEndingRecord
            {
                IsResolved = true,
                EndingId = ending.Id,
                HeroineId = ending.HeroineId,
                ResultFlag = ending.ResultFlag,
                Succeeded = ending.IsSuccess,
                EvaluatedYear = data.Year,
                EvaluatedMonth = data.Month,
                EvaluatedDay = data.Day
            };
            AddFlag(data, ending.ResultFlag);
            data.Revision++;
            return new AnnualProgressResult
            {
                DefinitionId = ending.Id,
                ResultFlag = ending.ResultFlag,
                Succeeded = ending.IsSuccess,
                Applied = true
            };
        }

        public IReadOnlyList<AnnualProgressResult> ResolveDueMilestones(GamePlayerData data)
        {
            RequireState(data);
            data.SeasonGoalResults ??= new List<SeasonGoalRecord>();
            var currentDate = CurrentDate(data);
            var results = new List<AnnualProgressResult>();
            foreach (var goal in definition.SeasonGoals
                         .Where(value => value.Deadline <= currentDate)
                         .OrderBy(value => value.Deadline))
            {
                if (data.SeasonGoalResults.Any(value => value.GoalId == goal.Id)) continue;
                results.Add(ResolveSeasonGoal(data, goal.SeasonId));
            }
            if (currentDate == definition.EndDate && data.AnnualEndingResult?.IsResolved != true)
                results.Add(ResolveEnding(data));
            return results;
        }

        private void RequireState(GamePlayerData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var date = CurrentDate(data);
            if (date < definition.StartDate || date > definition.EndDate)
                throw new InvalidOperationException("Player date is outside the annual calendar");
            data.ActiveRouteFlags ??= new System.Collections.Generic.List<string>();
        }

        private static DateTime CurrentDate(GamePlayerData data) => new(data.Year, data.Month, data.Day);
        private static void AddFlag(GamePlayerData data, string flag)
        {
            if (!data.ActiveRouteFlags.Contains(flag)) data.ActiveRouteFlags.Add(flag);
        }
    }
}
