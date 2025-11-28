using DTO.Auth;
using TMPro;
using UnityEngine;

public class RecordController : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField nickname;

    [Header("Buttons")]
    public TMP2DButton saveButton;

    [Header("Texts")]
    public TMP_Text swordWinLose;
    public TMP_Text swordKillDeath;
    public TMP_Text swordDamage;
    public TMP_Text bowWinLose;
    public TMP_Text bowKillDeath;
    public TMP_Text bowDamage;
    public TMP_Text wandWinLose;
    public TMP_Text wandKillDeath;
    public TMP_Text wandDamage;

    RecordService recordService;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.ToFrontViewCam();
            }
            UIManager.Instance.Open(MenuId.MainMenu);
        }
    }

    private void OnEnable()
    {
        if (nickname != null)
            nickname.text = UserSession.Nickname;

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClick);

        recordService = new();
        GetData(Mode.SINGLE);

    }

    private void OnDisable()
    {
        if (nickname != null)
        {
            saveButton.onClick.RemoveAllListeners();
        }
    }

    private async void OnSaveClick()
    {
        if (nickname != null && !string.IsNullOrWhiteSpace(nickname.text))
        {
            var resp = await AccountManager.Instance.ChangeNickname(nickname.text);
            if (resp.status != 200)
            {
                nickname.text = UserSession.Nickname;
            }
        }
    }
    private async void GetData(Mode mode)
    {
        var resp = await recordService.GetRecord(mode);
        if (resp.status == 200)
        {
            var sword = resp.data.recordInfo[0];
            swordWinLose.text = $"{sword.win} / {sword.lose}";
            swordKillDeath.text = $"{sword.kill} /{sword.death}";
            swordDamage.text = $"{sword.damage / (sword.win + sword.lose)}";

            var bow = resp.data.recordInfo[1];
            bowWinLose.text = $"{bow.win} / {bow.lose}";
            bowKillDeath.text = $"{bow.kill} /{bow.death}";
            bowDamage.text = $"{bow.damage / (bow.win + bow.lose)}";

            var wand = resp.data.recordInfo[2];
            wandWinLose.text = $"{wand.win} / {wand.lose}";
            wandKillDeath.text = $"{wand.kill} /{wand.death}";
            wandDamage.text = $"{wand.damage / (wand.win + wand.lose)}";
        }
        else Debug.Log(resp.message);
    }
}
