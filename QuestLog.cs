using System;
using System.Collections.Generic;

namespace TileQuest
{
    public sealed class QuestLog
    {
        public const string AnyWeaponTarget = "Any Weapon";

        public QuestNode? Head { get; private set; }
        public QuestNode? Tail { get; private set; }
        public QuestNode? Current { get; private set; }
        public int Count { get; private set; }
        public int CompletedCount { get; private set; }
        public bool IsAllComplete => Current == null;
        public bool IsReadyForNightOne =>
            Current?.Kind == QuestKind.SurviveNight &&
            Current.Target is int night &&
            night == 1;
        public int CurrentIndex
        {
            get
            {
                if (Current == null)
                {
                    return Count;
                }

                int index = 0;
                for (QuestNode? node = Head; node != null; node = node.Next, index++)
                {
                    if (ReferenceEquals(node, Current))
                    {
                        return index;
                    }
                }

                throw new InvalidOperationException("The current quest is not in this quest log.");
            }
        }
        public int CurrentQuestIndex => CurrentIndex;
        public int CurrentProgress => Current?.Progress ?? 0;

        public event Action<QuestNode>? QuestCompleted;

        private int _nightsSurvived;
        private readonly Dictionary<string, long> _gatheredResources =
            new Dictionary<string, long>(StringComparer.Ordinal);
        private bool _isCatchingUpResources;

        public QuestLog()
        {
            Reset();
        }

        private QuestLog(bool initializeDefaults)
        {
        }

        public static QuestLog BuildDefaultChain()
        {
            var log = new QuestLog(initializeDefaults: false);
            log.AddLast(new QuestNode("Gather 10 Wood", QuestKind.GatherResource, "Wood", 10));
            log.AddLast(new QuestNode("Gather 5 Stone", QuestKind.GatherResource, "Stone", 5));
            log.AddLast(new QuestNode(
                "Craft your first weapon", QuestKind.CraftItem, AnyWeaponTarget, 1));
            for (int night = 1; night <= 5; night++)
            {
                log.AddLast(new QuestNode(
                    $"Survive Night {night}", QuestKind.SurviveNight, night, 1));
            }
            return log;
        }

        public void AddLast(QuestNode quest)
        {
            if (quest == null)
            {
                throw new ArgumentNullException(nameof(quest));
            }
            if (quest.Next != null)
            {
                throw new ArgumentException("A quest node already linked to another node cannot be appended.", nameof(quest));
            }
            if (ContainsNode(quest))
            {
                throw new ArgumentException("The quest node is already in this quest log.", nameof(quest));
            }

            if (Tail == null)
            {
                Head = Tail = Current = quest;
            }
            else
            {
                Tail.Next = quest;
                Tail = quest;
                if (Current == null)
                {
                    Current = quest;
                }
            }

            Count++;
            CatchUpCurrentNight();
            CatchUpCurrentResource();
        }

        public void InsertAfterCurrent(QuestNode quest)
        {
            if (Current == null)
            {
                throw new InvalidOperationException("Cannot insert after the current quest when the log is complete.");
            }
            InsertAfterCurrent(quest, Current);
        }

        public void InsertAfterCurrent(QuestNode quest, QuestNode anchor)
        {
            if (quest == null)
            {
                throw new ArgumentNullException(nameof(quest));
            }
            if (anchor == null)
            {
                throw new ArgumentNullException(nameof(anchor));
            }
            if (quest.Next != null)
            {
                throw new ArgumentException("A quest node already linked to another node cannot be inserted.", nameof(quest));
            }

            if (ContainsNode(quest))
            {
                throw new ArgumentException("The quest node is already in this quest log.", nameof(quest));
            }
            if (!ContainsNode(anchor))
            {
                throw new ArgumentException("The anchor quest is not in this quest log.", nameof(anchor));
            }

            quest.Next = anchor.Next;
            anchor.Next = quest;
            if (Tail == anchor)
            {
                Tail = quest;
            }
            Count++;
        }

        public bool CompleteCurrent()
        {
            if (Current == null)
            {
                return false;
            }

            QuestNode completed = Current;
            completed.MarkCompleted();
            CompletedCount++;
            QuestCompleted?.Invoke(completed);
            Current = completed.Next;
            CatchUpCurrentNight();
            CatchUpCurrentResource();
            return true;
        }

        public QuestNode? Find(Predicate<QuestNode> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            for (QuestNode? node = Head; node != null; node = node.Next)
            {
                if (predicate(node))
                {
                    return node;
                }
            }
            return null;
        }

        public IEnumerable<QuestNode> Enumerate()
        {
            for (QuestNode? node = Head; node != null; node = node.Next)
            {
                yield return node;
            }
        }

        public void OnResourceGathered(string type, int quantity)
        {
            if (quantity <= 0)
            {
                return;
            }

            _gatheredResources.TryGetValue(type, out long gathered);
            _gatheredResources[type] = gathered + quantity;
            CatchUpCurrentResource();
        }

        public void OnItemCrafted(string type)
        {
            if (Current?.Kind != QuestKind.CraftItem)
            {
                return;
            }

            bool targetMatches = string.Equals(Current.Target as string, type, StringComparison.Ordinal);
            bool anyWeapon = string.Equals(
                Current.Target as string, AnyWeaponTarget, StringComparison.Ordinal) &&
                ItemCatalog.CategoryOf(type) == ItemCategory.Weapon;
            if (!targetMatches && !anyWeapon)
            {
                return;
            }

            Current.SetProgress(Current.Progress + 1);
            CompleteIfReady();
        }

        public void OnEnemyKilled()
        {
            if (Current?.Kind != QuestKind.DefeatEnemies)
            {
                return;
            }

            Current.SetProgress(Current.Progress + 1);
            CompleteIfReady();
        }

        public void OnNightSurvived(int nightNumber)
        {
            if (nightNumber <= 0)
            {
                return;
            }

            _nightsSurvived = Math.Max(_nightsSurvived, nightNumber);
            CatchUpCurrentNight();
        }

        public void RestoreProgress(int index, int progress)
        {
            if (index < 0 || index > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Quest index is outside the log.");
            }
            if (progress < 0 || (index == Count && progress != 0))
            {
                throw new ArgumentOutOfRangeException(nameof(progress), progress, "Progress is outside the current quest.");
            }
            QuestNode? restoredCurrent = Head;
            for (int i = 0; i < index && restoredCurrent != null; i++)
            {
                restoredCurrent = restoredCurrent.Next;
            }
            if (restoredCurrent != null && progress > restoredCurrent.RequiredAmount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(progress), progress, "Progress cannot exceed the required amount.");
            }

            CompletedCount = 0;
            _nightsSurvived = 0;
            _gatheredResources.Clear();
            int currentIndex = 0;
            Current = Head;
            for (QuestNode? node = Head; node != null; node = node.Next, currentIndex++)
            {
                node.SetProgress(0);
                if (currentIndex < index)
                {
                    node.MarkCompleted();
                    CompletedCount++;
                    if (node.Kind == QuestKind.SurviveNight && node.Target is int survived)
                    {
                        _nightsSurvived = Math.Max(_nightsSurvived, survived);
                    }
                    Current = node.Next;
                }
                else if (currentIndex == index)
                {
                    node.SetProgress(progress);
                    if (node.IsCompleted)
                    {
                        CompletedCount++;
                        Current = node.Next;
                    }
                    break;
                }
            }

            if (index == Count)
            {
                Current = null;
            }

            CatchUpCurrentNight();
        }

        public IReadOnlyList<KeyValuePair<string, long>> GetGatheredResourceTotals()
        {
            return new List<KeyValuePair<string, long>>(_gatheredResources);
        }

        public void RestoreGatheredResourceTotals(IEnumerable<KeyValuePair<string, long>> totals)
        {
            if (totals == null)
            {
                throw new ArgumentNullException(nameof(totals));
            }

            _gatheredResources.Clear();
            foreach (KeyValuePair<string, long> total in totals)
            {
                if (ItemCatalog.CategoryOf(total.Key) != ItemCategory.Resource || total.Value <= 0)
                {
                    throw new ArgumentException("Gathered-resource totals must contain positive quantities of resources.", nameof(totals));
                }

                _gatheredResources[total.Key] = total.Value;
            }
            CatchUpCurrentResource();
        }

        public void Reset()
        {
            Head = null;
            Tail = null;
            Current = null;
            Count = 0;
            CompletedCount = 0;
            _nightsSurvived = 0;
            _gatheredResources.Clear();

            QuestLog defaults = BuildDefaultChain();
            foreach (QuestNode quest in defaults.Enumerate())
            {
                AddLast(new QuestNode(quest.Title, quest.Kind, quest.Target, quest.RequiredAmount));
            }
        }

        private void CompleteIfReady()
        {
            if (Current is QuestNode current && current.Progress >= current.RequiredAmount)
            {
                CompleteCurrent();
            }
        }

        private void CatchUpCurrentNight()
        {
            while (Current?.Kind == QuestKind.SurviveNight &&
                   Current.Target is int night &&
                   _nightsSurvived >= night)
            {
                Current.MarkCompleted();
                CompletedCount++;
                QuestNode completed = Current;
                QuestCompleted?.Invoke(completed);
                Current = completed.Next;
            }
        }

        private void CatchUpCurrentResource()
        {
            if (_isCatchingUpResources)
            {
                return;
            }

            _isCatchingUpResources = true;
            try
            {
                while (Current?.Kind == QuestKind.GatherResource &&
                       Current.Target is string type &&
                       _gatheredResources.TryGetValue(type, out long gathered) &&
                       gathered > 0)
                {
                    int needed = Current.RequiredAmount - Current.Progress;
                    int applied = (int)Math.Min(gathered, needed);
                    Current.SetProgress(Current.Progress + applied);

                    long remaining = gathered - applied;
                    if (remaining == 0)
                    {
                        _gatheredResources.Remove(type);
                    }
                    else
                    {
                        _gatheredResources[type] = remaining;
                    }

                    if (Current.Progress >= Current.RequiredAmount)
                    {
                        CompleteCurrent();
                    }
                }
            }
            finally
            {
                _isCatchingUpResources = false;
            }
        }

        private bool ContainsNode(QuestNode quest)
        {
            for (QuestNode? node = Head; node != null; node = node.Next)
            {
                if (ReferenceEquals(node, quest))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
