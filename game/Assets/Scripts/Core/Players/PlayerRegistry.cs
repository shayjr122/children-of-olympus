using System;
using System.Collections.Generic;

namespace GMR.Core.Players
{
    /// <summary>
    /// All Players in the current Encounter (AD-4). Lives on EncounterContext; nothing is static, so a second
    /// Player is just a second entry.
    /// </summary>
    public sealed class PlayerRegistry
    {
        readonly List<IPlayerTarget> _players = new List<IPlayerTarget>(4);

        public int Count => _players.Count;

        /// <summary>Players in registration order.</summary>
        public IReadOnlyList<IPlayerTarget> All => _players;

        public void Register(IPlayerTarget player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            for (int i = 0; i < _players.Count; i++)
            {
                if (ReferenceEquals(_players[i], player)) return;
                if (_players[i].Id == player.Id)
                    throw new InvalidOperationException($"A Player with id {player.Id} is already registered.");
            }
            _players.Add(player);
        }

        public bool Unregister(IPlayerTarget player) => _players.Remove(player);

        public bool TryGet(PlayerId id, out IPlayerTarget player)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].Id == id)
                {
                    player = _players[i];
                    return true;
                }
            }
            player = null;
            return false;
        }
    }
}
