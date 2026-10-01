using System.Collections.Concurrent;
using VerintCsharpMcp.Models;

namespace VerintCsharpMcp.Services;

public class PendingActionStore
{
    private readonly ConcurrentDictionary<string, PendingAction>
        _actions = new();

    public void Add(PendingAction action)
    {
        _actions[action.ActionId] = action;
    }

    public PendingAction? Get(string actionId)
    {
        _actions.TryGetValue(
            actionId,
            out var action);

        return action;
    }

    public bool Remove(string actionId)
    {
        return _actions.TryRemove(
            actionId,
            out _);
    }

    /// <summary>
    /// Atomically removes and returns a pending action.
    /// This prevents two concurrent confirmations from
    /// executing the same action.
    /// </summary>
    public bool TryClaim(
        string actionId,
        out PendingAction? action)
    {
        return _actions.TryRemove(
            actionId,
            out action);
    }
}
