using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CouncilPanel : MonoBehaviour
{
    public CanvasGroup Group;

    public GridLayoutGroup LayoutGroup;

    public CouncilPlayerEntry PlayerEntryPrefab;

    private List<CouncilPlayerEntry> _playerEntries = new List<CouncilPlayerEntry>();

    // Start is called before the first frame update
    void Start()
    {
        SetGroupActive(false);
    }

    private void OnEnable()
    {
        GameManager.OnPlayerReport += OnPlayerReport;
        GameManager.OnVoteFinished += OnVoteFinished;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerReport -= OnPlayerReport;
        GameManager.OnVoteFinished -= OnVoteFinished;
    }

    private void OnVoteFinished()
    {
        SetGroupActive(false);
    }

    private void OnPlayerReport(PlayerBehaviour obj)
    {
        SetGroupActive(true);

        foreach(Transform t in LayoutGroup.transform)
        {
            Destroy(t.gameObject);
        }
        _playerEntries.Clear();

        List<PlayerBehaviour> players = GameManager.Instance.Players;
        players.Sort((a, b) => a.State == PlayerState.Alive ? -1 : 1 );

        foreach (PlayerBehaviour player in GameManager.Instance.Players)
        {
            CouncilPlayerEntry entry = Instantiate(PlayerEntryPrefab, LayoutGroup.transform);
            entry.Council = this;
            entry.PlayerBinding = player;
            entry.RootButton.interactable = player.State == PlayerState.Alive;

            _playerEntries.Add(entry);
        }
    }

    internal void EntryClick(CouncilPlayerEntry councilPlayerEntry)
    {
        PlayerState state = PlayerBehaviour.Local.State;
        foreach (CouncilPlayerEntry entry in _playerEntries)
        {
            if(entry != councilPlayerEntry)
            {
                entry.AcceptButton.gameObject.SetActive(false);
                entry.CancelButton.gameObject.SetActive(false);
            }
            else if (state == PlayerState.Alive && !GameManager.Instance.AlreadyVoted(PlayerBehaviour.Local))
            {
                entry.AcceptButton.gameObject.SetActive(true);
                entry.CancelButton.gameObject.SetActive(true);
            }
        }
    }

    internal void SetGroupActive(bool state)
    {
        Group.alpha = state ? 1 : 0;
        Group.interactable = state;
        Group.blocksRaycasts = state;
    }
}
