using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SelectButtonUI : MonoBehaviour
{
    [SerializeField] private GameObject unfocus, focus;
    [SerializeField] private Button button;
    [SerializeField] private SelectButtonUI[] otherButtons;

    private void OnValidate()
    {
        // unfocus = transform.GetChild(0).gameObject;
        // focus = transform.GetChild(1).gameObject;
        // button = GetComponent<Button>();
    }

    private void Start()
    {
        button.onClick.AddListener(SelectFocus);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(SelectFocus);
    }

    public void SelectFocus()
    {
        foreach (var b in otherButtons)
        {
            b.SelectUnfocus();
        }

        focus.SetActive(true);
        unfocus.SetActive(false);
    }

    public void SelectUnfocus()
    {
        unfocus.SetActive(true);
        focus.SetActive(false);
    }

    // public void OnPointerClick(PointerEventData eventData)
    // {
    //     SelectFocus();
    // }
}
