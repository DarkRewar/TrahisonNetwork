using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerRole
{
    Crewmate = 0,
    Impostor
}

public enum PlayerState
{
    Alive,
    Dead,
    Discovered
}

public class PlayerInfos
{
    public string PlayerName;
    public Color PlayerColor;
}

public class PlayerBehaviour : NetworkBehaviour
{
    public static PlayerBehaviour Local;

    [SyncVar(hook = nameof(OnCanMoveChanged))]
    public bool CanMove = true;
    public float MoveSpeed = 2;

    public float KillDistance = 2;
    public float ReportDistance = 2;

    [SyncVar]
    protected internal PlayerRole Role = PlayerRole.Crewmate;

    [SyncVar(hook = nameof(OnPlayerStateChanged))]
    protected internal PlayerState State = PlayerState.Alive;

    [SyncVar(hook = nameof(OnPlayerColorChanged))]
    public Color PlayerColor;

    [SerializeField]
    private SpriteRenderer _renderer;

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private Collider2D _collider;

    [SerializeField]
    private Rigidbody2D _rigidbody;
    public Rigidbody2D Rigidbody => _rigidbody;

    [SerializeField]
    private NetworkTransform _networkTransform;
    public NetworkTransform NetworkTransform => _networkTransform;

    private Vector2 _movementInput;

    private Camera _camera;

    /// <summary>
    /// Le joueur que l'on peut tuer.
    /// </summary>
    private PlayerBehaviour _killPlayer;
    public bool CanKill => _killPlayer != null;

    /// <summary>
    /// Le joueur que l'on peut report.
    /// </summary>
    private PlayerBehaviour _reportPlayer;
    public bool CanReport => _reportPlayer != null;

    public bool IsAlive => State == PlayerState.Alive;

    public event Action<PlayerBehaviour, PlayerBehaviour> OnPlayerCanKill;

    public event Action<PlayerBehaviour> OnPlayerVoted;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        if (isLocalPlayer)
        {
            Local = this;
            _camera = Camera.main;
        }

        if (GameManager.Instance)
        {
            GameManager.Instance.AddPlayer(this);
        }

        if (isServer) 
        {
            PlayerInfos infos = (PlayerInfos)connectionToClient.authenticationData;
            if(infos != null)
            {
                PlayerColor = infos.PlayerColor;
                _renderer.color = infos.PlayerColor;
            }

            if (GameManager.Instance)
            {
                CanMove = false;
                yield return new WaitForSeconds(5);
                CanMove = true;
            }
        }
    }

    internal void SetColor(Color color)
    {
        PlayerInfos infos = (PlayerInfos)connectionToClient.authenticationData;
        if(infos == null)
        {
            infos = new PlayerInfos();
            connectionToClient.authenticationData = infos;
        }

        ((PlayerInfos)connectionToClient.authenticationData).PlayerColor = color;
        PlayerColor = color;
        _renderer.color = color;
    }

    internal void OnPlayerColorChanged(Color oldColor, Color newColor)
    {
        _renderer.color = newColor;
    }

    #region MOVEMENT

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer || !CanMove) return;

        _movementInput = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );

        _animator.SetBool("IsWalking", !Mathf.Approximately(_movementInput.magnitude, 0));

        if(_movementInput.x > 0 && _renderer.flipX)
        {
            _renderer.flipX = false;
            CmdFlipX(false);
        }
        else if(_movementInput.x < 0 && !_renderer.flipX)
        {
            _renderer.flipX = true;
            CmdFlipX(true);
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer || !CanMove) return;

        _rigidbody.velocity = MoveSpeed * _movementInput;

        Collider2D[] cols = Physics2D.OverlapCircleAll(_rigidbody.position, 2, LayerMask.GetMask("Player"));

        foreach (Collider2D col in cols)
        {
            if (col.gameObject == gameObject) continue;

            if (col && col.gameObject.CompareTag("Player") && col.gameObject.TryGetComponent(out PlayerBehaviour player))
            {
                if (Role == PlayerRole.Impostor && player.State == PlayerState.Alive)
                {
                    _killPlayer = player;
                    //OnPlayerCanKill?.Invoke(this, player);
                }
                
                if (player.State == PlayerState.Dead)
                {
                    _reportPlayer = player;
                }
            }
            //else
            //{
            //    _killPlayer = null;
            //    //OnPlayerCanKill?.Invoke(this, null);
            //}
        }

        if (_killPlayer && Vector2.Distance(_killPlayer.Rigidbody.position, _rigidbody.position) > KillDistance)
        {
            _killPlayer = null;
        }

        if (_reportPlayer && Vector2.Distance(_reportPlayer.Rigidbody.position, _rigidbody.position) > ReportDistance)
        {
            _reportPlayer = null;
        }
    }

    [Command]
    private void CmdFlipX(bool v)
    {
        if(!isLocalPlayer)
            _renderer.flipX = v;

        RpcFlipX(v);
    }

    [ClientRpc]
    private void RpcFlipX(bool v)
    {
        if(!isLocalPlayer)
            _renderer.flipX = v;
    }

    private void LateUpdate()
    {
        if (!isLocalPlayer || !_camera) return;

        _camera.transform.position = transform.position + 10 * Vector3.back;
    }

    internal void OnCanMoveChanged(bool oldVal, bool newVal)
    {
        if (!newVal)
        {
            _rigidbody.velocity = Vector2.zero;
        }
    }

    #endregion

    #region KILL

    internal void ProcessKill()
    {
        CmdAskKill(_killPlayer);
    }

    [Command]
    private void CmdAskKill(PlayerBehaviour killPlayer)
    {
        if(Vector3.Distance(killPlayer.transform.position, transform.position) <= 2)
        {
            _networkTransform.ServerTeleport(killPlayer.transform.position);
            //transform.position = killPlayer.transform.position;
            killPlayer.State = PlayerState.Dead;
            killPlayer.CanMove = false;
            //killPlayer.RpcKill();
        }
    }

    private void OnPlayerStateChanged(PlayerState oldState, PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.Dead:
                _rigidbody.velocity = Vector2.zero;
                _movementInput = Vector2.zero;
                _animator.SetBool("IsDead", true);
                break;
            case PlayerState.Discovered:
                _renderer.enabled = false;
                _collider.enabled = false;
                _rigidbody.simulated = false;
                break;
        }
    }

    #endregion

    #region REPORT

    internal void Report()
    {
        if (CanReport)
            CmdAskReport(_reportPlayer);
    }

    [Command]
    private void CmdAskReport(PlayerBehaviour reportPlayer)
    {
        float distance = Vector2.Distance(reportPlayer.transform.position, transform.position);
        if(reportPlayer.State == PlayerState.Dead && distance <= ReportDistance)
        {
            GameManager.Instance.ReportPlayerServer(reportPlayer, this);
            reportPlayer.RpcReport(this);
        }
    }

    [ClientRpc]
    internal void RpcReport(PlayerBehaviour reportedBy)
    {
        GameManager.Instance.ReportPlayerClient(this, reportedBy);
    }

    #endregion

    #region VOTE

    [Command]
    public void CmdVote(PlayerBehaviour playerVoted)
    {
        if(GameManager.Instance.Vote(this, playerVoted))
        {
            RpcVote(playerVoted);
            GameManager.Instance.CheckAllVotes();
        }
    }

    [ClientRpc]
    public void RpcVote(PlayerBehaviour playerVoted)
    {
        GameManager.Instance.Vote(this, playerVoted);
        OnPlayerVoted?.Invoke(playerVoted);
    }

    #endregion
}
