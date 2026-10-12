using System;

namespace TileQuest
{
    public enum QuestKind
    {
        GatherResource,
        CraftItem,
        DefeatEnemies,
        SurviveNight,
    }

    public sealed class QuestNode
    {
        public string Title { get; }
        public QuestKind Kind { get; }
        public object Target { get; }
        public int RequiredAmount { get; }
        public int Progress { get; private set; }
        public bool IsCompleted { get; private set; }
        public QuestNode? Next { get; set; }

        public QuestNode(string title, QuestKind kind, object target, int requiredAmount)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A quest title is required.", nameof(title));
            }
            if (requiredAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredAmount), requiredAmount, "Required amount must be positive.");
            }

            bool validTarget = kind switch
            {
                QuestKind.GatherResource or QuestKind.CraftItem => target is string,
                QuestKind.DefeatEnemies or QuestKind.SurviveNight => target is int,
                _ => false,
            };
            if (!validTarget)
            {
                throw new ArgumentException("The target type does not match the quest kind.", nameof(target));
            }

            Title = title;
            Kind = kind;
            Target = target;
            RequiredAmount = requiredAmount;
        }

        internal void SetProgress(int progress)
        {
            Progress = Math.Clamp(progress, 0, RequiredAmount);
            IsCompleted = Progress >= RequiredAmount;
        }

        internal void MarkCompleted()
        {
            Progress = RequiredAmount;
            IsCompleted = true;
        }
    }
}
