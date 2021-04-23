using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CouncilPlayerEntry : MonoBehaviour
{
    public Image PlayerAvatar;
    public TMP_Text PlayerName;
    public GameObject VotedImage;

    public Button RootButton;

    public Button CancelButton;
    public Button AcceptButton;

    public CouncilPanel Council;
    public PlayerBehaviour PlayerBinding;

    // Start is called before the first frame update
    void Start()
    {
        RootButton.onClick.AddListener(RootButtonClicked);

        CancelButton.onClick.AddListener(CancelButtonClicked);
        AcceptButton.onClick.AddListener(AcceptButtonClicked);

        VotedImage.gameObject.SetActive(false);
        AcceptButton.gameObject.SetActive(false);
        CancelButton.gameObject.SetActive(false);

        PlayerAvatar.color = PlayerBinding.PlayerColor;
        PlayerBinding.OnPlayerVoted += OnPlayerVoted;
    }

    private void CancelButtonClicked()
    {
        AcceptButton.gameObject.SetActive(false);
        CancelButton.gameObject.SetActive(false);
    }

    private void RootButtonClicked()
    {
        Council.EntryClick(this);
    }

    private void AcceptButtonClicked()
    {
        PlayerBehaviour.Local.CmdVote(PlayerBinding);
    }

    private void OnPlayerVoted(PlayerBehaviour obj)
    {
        VotedImage.gameObject.SetActive(true);
        AcceptButton.gameObject.SetActive(false);
        CancelButton.gameObject.SetActive(false);
    }
}
