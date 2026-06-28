using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SudokuGame
{
   
    public class SudokuCell : MonoBehaviour
    {
        [Header("UI посилання")]
        public Button Button;
        public TextMeshProUGUI Label;
        public Image Background;

        [Header("Кольори станів")]
        public Color NormalColor = Color.white;
        public Color SelectedColor = new Color(0.70f, 0.85f, 1f);
        public Color SameNumberColor = new Color(0.85f, 0.92f, 1f);
        public Color FixedTextColor = new Color(0.1f, 0.1f, 0.1f);
        public Color UserTextColor = new Color(0.10f, 0.35f, 0.85f);
        public Color ErrorColor = new Color(1f, 0.55f, 0.55f);
        public Color HintColor = new Color(0.65f, 0.9f, 0.65f);

        public int Row { get; private set; }
        public int Col { get; private set; }
        public int Value { get; private set; }
        public bool IsFixedCell { get; private set; }
        public bool HasError { get; private set; }

        private SudokuGameManager _manager;

        public void Init(int row, int col, SudokuGameManager manager)
        {
            Row = row;
            Col = col;
            _manager = manager;
            Button.onClick.AddListener(OnCellClicked);
        }

        private void OnCellClicked()
        {
            _manager.SelectCell(this);
        }

        
        public void SetValue(int value, bool isFixed)
        {
            Value = value;
            IsFixedCell = isFixed;
            HasError = false;

            Label.text = value == 0 ? "" : value.ToString();
            Label.color = isFixed ? FixedTextColor : UserTextColor;

            UpdateBackground(false, false);
        }

        public void SetError(bool hasError)
        {
            HasError = hasError;
            UpdateBackground(false, false);
        }

        public void ShowHintFlash()
        {
            if (Background != null)
                Background.color = HintColor;
        }

       
        public void UpdateBackground(bool isSelected, bool sameNumberHighlight)
        {
            if (Background == null) return;

            if (HasError)
                Background.color = ErrorColor;
            else if (isSelected)
                Background.color = SelectedColor;
            else if (sameNumberHighlight)
                Background.color = SameNumberColor;
            else
                Background.color = NormalColor;
        }

        public bool IsEmpty => Value == 0;
    }
}