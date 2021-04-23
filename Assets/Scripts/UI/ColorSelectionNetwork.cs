using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ColorEntry
{
    public PlayerBehaviour PlayerAssigned;
    public int ColorIndex;
}

public class ColorSelectionNetwork : NetworkBehaviour
{
    public List<Color> GameColors;

    public SyncList<ColorEntry> ColorSelected = new SyncList<ColorEntry>();

    public Transform VerticalLayout;
    public Button ColorButtonPrefab;

    private List<Button> _buttons = new List<Button>();

    // Start is called before the first frame update
    void Start()
    {
        ColorSelected.Callback += OnColorSelectedChanged;

        foreach (Color color in GameColors)
        {
            Button btn = Instantiate(ColorButtonPrefab, VerticalLayout);
            btn.image.color = color;
            btn.onClick.AddListener(() => SelectColor(color));
            _buttons.Add(btn);
        }
    }

    private void OnColorSelectedChanged(SyncList<ColorEntry>.Operation op, int itemIndex, ColorEntry oldItem, ColorEntry newItem)
    {
        _buttons.ForEach(x => x.interactable = true);
        foreach(ColorEntry entry in ColorSelected)
        {
            _buttons[entry.ColorIndex].interactable = false;
        }
    }

    private void SelectColor(Color color)
    {
        PlayerBehaviour player = PlayerBehaviour.Local;
        int colorIndex = GameColors.FindIndex(x => x == color);
        CmdAskForColor(colorIndex, player);
    }

    [Command(requiresAuthority = false)]
    public void CmdAskForColor(int color, PlayerBehaviour player)
    {
        int entryIndex = ColorSelected.FindIndex(x => x.PlayerAssigned == player);
        if(entryIndex > -1)
        {
            ColorSelected.RemoveAt(entryIndex);
        }

        ColorEntry entry = new ColorEntry
        {
            PlayerAssigned = player,
            ColorIndex = color
        };
        ColorSelected.Add(entry);

        player.SetColor(GameColors[color]);
    }
}
