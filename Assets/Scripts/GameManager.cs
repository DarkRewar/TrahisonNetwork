using Mirror;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct StartGameMessage : NetworkMessage
{

}

public struct VoteEndMessage : NetworkMessage
{
    public PlayerBehaviour PlayerDead;
}

public struct PlayerVote
{
    /// <summary>
    /// Le joueur qui a voté
    /// </summary>
    public PlayerBehaviour Player;

    /// <summary>
    /// Le joueur qui a été voté
    /// </summary>
    public PlayerBehaviour Target;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private void Awake()
    {
        Instance = this;
    }

    public static Action OnGameStarted;

    public static event Action<PlayerBehaviour> OnPlayerReport;
    public static event Action OnVoteFinished;

    private List<PlayerBehaviour> _players = new List<PlayerBehaviour>();
    public List<PlayerBehaviour> Players => _players;

    private List<PlayerVote> _voted = new List<PlayerVote>();

    // Use this for initialization
    IEnumerator Start()
    {
        NetworkClient.RegisterHandler<StartGameMessage>(OnStartGameReceived);
        NetworkClient.RegisterHandler<VoteEndMessage>(OnVoteEndReceived);

        while (!NetworkServer.active)
        {
            yield return null;
        }

        while(_players.Count != NetworkServer.connections.Count || !_players.TrueForAll(x => x.connectionToClient.isReady))
        {
            yield return null;
        }

        int rand = UnityEngine.Random.Range(0, _players.Count);
        _players[rand].Role = PlayerRole.Impostor;
        //_players[0].Role = PlayerRole.Impostor;

        yield return new WaitForSeconds(1);

        NetworkServer.SendToReady(new StartGameMessage());
        //if (NetworkClient.active)
        //{
        //    OnGameStarted?.Invoke();
        //}
    }

    public void AddPlayer(PlayerBehaviour player)
    {
        _players.Add(player);
    }

    private void OnStartGameReceived(StartGameMessage msg)
    {
        OnGameStarted?.Invoke();
    }

    internal void ReportPlayerServer(PlayerBehaviour deadPlayer, PlayerBehaviour reportedBy)
    {
        foreach(PlayerBehaviour player in _players)
        {
            player.CanMove = false;

            if(player.State == PlayerState.Alive)
            {
                Transform startPos = NetworkManager.singleton.GetStartPosition();
                player.NetworkTransform.ServerTeleport(startPos.position);
            }            
        }
    }

    internal void ReportPlayerClient(PlayerBehaviour deadPlayer, PlayerBehaviour reportedBy)
    {
        OnPlayerReport?.Invoke(reportedBy);
    }

    #region VOTE 

    internal bool AlreadyVoted(PlayerBehaviour local)
    {
        return _voted.FindIndex(x => x.Player == local) > -1;
    }

    internal bool Vote(PlayerBehaviour origin, PlayerBehaviour target)
    {
        if (origin.State == PlayerState.Alive && target.State == PlayerState.Alive && !AlreadyVoted(origin))
        {
            _voted.Add(new PlayerVote { Player = origin, Target = target });
            return true;
        }

        return false;
    }

    internal void CheckAllVotes()
    {
        int aliveplayers = _players.Count(x => x.State == PlayerState.Alive);
        if(aliveplayers == _voted.Count)
        {
            foreach (PlayerBehaviour player in _players)
            {
                if (player.IsAlive)
                {
                    player.CanMove = true;
                }
            }

            Dictionary<PlayerBehaviour, int> votes = new Dictionary<PlayerBehaviour, int>();
            foreach(PlayerVote vote in _voted)
            {
                if (votes.ContainsKey(vote.Target))
                {
                    ++votes[vote.Target];
                }
                else
                {
                    votes.Add(vote.Target, 1);
                }
            }
            List<KeyValuePair<PlayerBehaviour, int>> order = votes.OrderByDescending(x => x.Value).ToList();
            if(order.Count() <= 1 || order[0].Value != order[1].Value)
            {
                PlayerBehaviour player = order[0].Key;
                player.CanMove = false;
                player.State = PlayerState.Dead;

                NetworkServer.SendToAll(new VoteEndMessage { PlayerDead = player });
            }
            else
            {
                NetworkServer.SendToAll(new VoteEndMessage { PlayerDead = null });
            }
        }
    }

    private void OnVoteEndReceived(VoteEndMessage arg2)
    {
        OnVoteFinished?.Invoke();
    }

    #endregion
}