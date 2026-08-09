using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image coverImage;
    [SerializeField] private TMP_Text songNameText;
    [SerializeField] private TMP_Text artistText;
    [SerializeField] private Color placeholderCoverColor = new(0.16f, 0.22f, 0.34f, 1f);

    private SongDefinition song;
    private Action<SongDefinition> onSelected;

    public void Bind(SongDefinition songDefinition, Action<SongDefinition> selectedCallback)
    {
        song = songDefinition;
        onSelected = selectedCallback;

        if (song == null)
        {
            Debug.LogError("SongCardView cannot bind a null song.", this);
            button.interactable = false;
            return;
        }

        songNameText.text = song.DisplayName;
        artistText.text = song.Artist;
        coverImage.sprite = song.CoverSprite;
        coverImage.color = song.CoverSprite != null ? Color.white : placeholderCoverColor;

        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        onSelected?.Invoke(song);
    }
}
