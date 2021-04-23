using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    public Button UseBtn;
    public Button KillBtn;
    public Button ReportBtn;

    private PlayerBehaviour _player;

    // Start is called before the first frame update
    void Start()
    {
        ReportBtn.gameObject.SetActive(false);
        ReportBtn.onClick.AddListener(OnReportClicked);
        UseBtn.interactable = false;
        KillBtn.interactable = false;
        KillBtn.onClick.AddListener(KillBtnClick);

        GameManager.OnGameStarted += OnGameStart;
    }

    private void OnDestroy()
    {
        GameManager.OnGameStarted -= OnGameStart;
    }

    private void OnGameStart()
    {
        _player = PlayerBehaviour.Local;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(_player)
        {
            UseBtn.gameObject.SetActive(_player.Role == PlayerRole.Crewmate);
            KillBtn.gameObject.SetActive(_player.Role == PlayerRole.Impostor);

            if (_player.Role == PlayerRole.Impostor)
                KillBtn.interactable = _player.CanKill;

            ReportBtn.gameObject.SetActive(_player.CanReport);
            ReportBtn.interactable = _player.CanReport;
        }
    }

    private void KillBtnClick()
    {
        if (_player && _player.Role == PlayerRole.Impostor)
        {
            _player.ProcessKill();
        }
    }

    private void OnReportClicked()
    {
        if(_player && _player.CanReport)
        {
            _player.Report();
        }
    }
}
