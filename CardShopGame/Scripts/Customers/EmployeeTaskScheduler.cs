using System;
using System.Collections.Generic;
using UnityEngine;

public enum TaskPriority { Low, Medium, High, Critical }

/// <summary>
/// Central task queue for all employees.
/// Other systems register tasks; employees request the next unassigned task.
/// Tasks are sorted by priority so Critical work is always handled first.
/// </summary>
public class EmployeeTaskScheduler : MonoBehaviour
{
    public static EmployeeTaskScheduler Instance { get; private set; }

    [Serializable]
    public class StoreTask
    {
        public string       id;
        public Vector3      targetPosition;
        public TaskPriority priority;
        public bool         assigned;
        public Action       onComplete;
        public string       description;
    }

    private readonly List<StoreTask> _queue = new();
    public IReadOnlyList<StoreTask>  Queue => _queue;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    public void RegisterTask(Vector3 pos, TaskPriority priority,
                             Action onComplete = null, string description = "")
    {
        _queue.Add(new StoreTask
        {
            id            = Guid.NewGuid().ToString(),
            targetPosition = pos,
            priority      = priority,
            assigned      = false,
            onComplete    = onComplete,
            description   = description,
        });
        Sort();
    }

    /// <summary>Called by EmployeeAI to claim the next open task.</summary>
    public StoreTask ClaimNext()
    {
        foreach (var task in _queue)
        {
            if (!task.assigned)
            {
                task.assigned = true;
                return task;
            }
        }
        return null;
    }

    public void CompleteTask(string id)
    {
        var task = _queue.Find(t => t.id == id);
        if (task == null) return;
        task.onComplete?.Invoke();
        _queue.Remove(task);
        Sort();
    }

    public void AbandonTask(string id)
    {
        var task = _queue.Find(t => t.id == id);
        if (task != null) task.assigned = false;  // back in pool
    }

    private void Sort() =>
        _queue.Sort((a, b) => b.priority.CompareTo(a.priority));
}
