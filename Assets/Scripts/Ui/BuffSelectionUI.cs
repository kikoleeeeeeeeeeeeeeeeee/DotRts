using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuffSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject buffPanel;

    [Header("Buff Option Buttons")]
    [SerializeField] private Button buffButton1;
    [SerializeField] private Button buffButton2;
    [SerializeField] private Button buffButton3;
    [SerializeField] private Text buffButton1Text;
    [SerializeField] private Text buffButton2Text;
    [SerializeField] private Text buffButton3Text;

    private Color normalTextColor = Color.white;
    private Color selectedTextColor = Color.yellow;

    private void OnEnable()
    {
        // 事件注册已经在 Start 中完成，这里不再重复注册
        // 确保 BuffManager 实例存在
        if (BuffManager.Instance == null)
        {
            BuffManager existingBuffManager = FindObjectOfType<BuffManager>();
            if (existingBuffManager == null)
            {
                GameObject buffManagerObj = new GameObject("BuffManager");
                buffManagerObj.AddComponent<BuffManager>();
            }
        }
    }

    private void OnDisable()
    {
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.OnBuffSelectionStarted -= BuffManager_OnBuffSelectionStarted;
            BuffManager.Instance.OnBuffSelected -= BuffManager_OnBuffSelected;
        }
    }

    private void Start()
    {
        if (buffPanel != null)
        {
            buffPanel.SetActive(false);
        }

        // 为三个按钮添加点击事件
        if (buffButton1 != null)
        {
            buffButton1.onClick.AddListener(() => OnBuffOptionSelected(0));
        }
        if (buffButton2 != null)
        {
            buffButton2.onClick.AddListener(() => OnBuffOptionSelected(1));
        }
        if (buffButton3 != null)
        {
            buffButton3.onClick.AddListener(() => OnBuffOptionSelected(2));
        }

        // 保存默认文本颜色
        if (buffButton1Text != null) normalTextColor = buffButton1Text.color;
        
        // 注册事件（只在 Start 时注册一次）
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.OnBuffSelectionStarted += BuffManager_OnBuffSelectionStarted;
            BuffManager.Instance.OnBuffSelected += BuffManager_OnBuffSelected;
        }
    }

    // 鼠标悬停事件处理
    public void OnPointerEnterButton1()
    {
        if (buffButton1Text != null)
        {
            buffButton1Text.color = selectedTextColor;
        }
    }

    public void OnPointerExitButton1()
    {
        if (buffButton1Text != null)
        {
            buffButton1Text.color = normalTextColor;
        }
    }

    public void OnPointerEnterButton2()
    {
        if (buffButton2Text != null)
        {
            buffButton2Text.color = selectedTextColor;
        }
    }

    public void OnPointerExitButton2()
    {
        if (buffButton2Text != null)
        {
            buffButton2Text.color = normalTextColor;
        }
    }

    public void OnPointerEnterButton3()
    {
        if (buffButton3Text != null)
        {
            buffButton3Text.color = selectedTextColor;
        }
    }

    public void OnPointerExitButton3()
    {
        if (buffButton3Text != null)
        {
            buffButton3Text.color = normalTextColor;
        }
    }

    private void BuffManager_OnBuffSelectionStarted(object sender, System.EventArgs e)
    {
        ShowBuffSelection();
    }

    private void BuffManager_OnBuffSelected(object sender, BuffData buffData)
    {
        HideBuffSelection();
        
        // buff 选择完成后，进入下一波
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ConfirmBuffSelectionAndNextWave();
        }
    }

    // 公共方法，让 BuffManager 直接调用
    public void ShowBuffSelectionUI()
    {
        ShowBuffSelection();
    }

    private void ShowBuffSelection()
    {
        if (buffPanel == null)
        {
            return;
        }

        // 检查并尝试创建 BuffManager
        if (BuffManager.Instance == null)
        {
            // 尝试在场景中查找 BuffManager
            BuffManager existingBuffManager = FindObjectOfType<BuffManager>();
            if (existingBuffManager != null)
            {
                // 直接使用反射设置实例
                System.Reflection.FieldInfo field = typeof(BuffManager).GetField("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(null, existingBuffManager);
                }
            }
            else
            {
                // 创建新的 BuffManager
                GameObject buffManagerObj = new GameObject("BuffManager");
                buffManagerObj.AddComponent<BuffManager>();
            }
        }

        // 再次检查实例是否设置成功
        if (BuffManager.Instance == null)
        {
            return;
        }

        List<BuffData> buffOptions = BuffManager.Instance.GetCurrentBuffOptions();

        // 更新三个按钮的文本
        UpdateBuffButton(buffOptions, 0, buffButton1, buffButton1Text);
        UpdateBuffButton(buffOptions, 1, buffButton2, buffButton2Text);
        UpdateBuffButton(buffOptions, 2, buffButton3, buffButton3Text);

        // 强制设置为 active
        buffPanel.SetActive(true);
        // 确保父对象也处于激活状态
        if (buffPanel.transform.parent != null)
        {
            buffPanel.transform.parent.gameObject.SetActive(true);
        }
        Time.timeScale = 0;
    }

    private void HideBuffSelection()
    {
        buffPanel.SetActive(false);
        Time.timeScale = 1;
    }

    private void UpdateBuffButton(List<BuffData> buffOptions, int index, Button button, Text text)
    {
        if (button == null)
        {
            return;
        }
        if (text == null)
        {
            return;
        }

        if (index < buffOptions.Count)
        {
            BuffData buffData = buffOptions[index];
            // 只显示 Buff 的介绍
            text.text = buffData.description;
            text.color = normalTextColor; // 重置为默认颜色
            button.gameObject.SetActive(true);
        }
        else
        {
            button.gameObject.SetActive(false);
        }
    }

    private string GetBuffEffectText(BuffData buffData)
    {
        string effect = "";
        switch (buffData.buffType)
        {
            case BuffType.AttackDamage:
                effect = $"攻击力 +{buffData.value}%";
                break;
            case BuffType.AttackSpeed:
                effect = $"攻击速度 +{buffData.value}%";
                break;
            case BuffType.Health:
                effect = $"生命值 +{buffData.value}%";
                break;
            case BuffType.MoveSpeed:
                effect = $"移动速度 +{buffData.value}%";
                break;
            case BuffType.AttackRange:
                effect = $"攻击范围 +{buffData.value}%";
                break;
            case BuffType.ProductionCount:
                effect = $"生产数量 +{buffData.value}%";
                break;
            case BuffType.ResourceGain:
                effect = $"资源获取 +{buffData.value}%";
                break;
            case BuffType.DirectResource:
                effect = $"直接获得 {buffData.value} 资源";
                break;
        }
        return effect;
    }

    private void OnBuffOptionSelected(int optionIndex)
    {
        // 高亮显示选中的按钮文本
        HighlightSelectedButton(optionIndex);
        BuffManager.Instance?.SelectBuff(optionIndex);
    }

    private void HighlightSelectedButton(int optionIndex)
    {
        // 重置所有按钮文本颜色
        if (buffButton1Text != null) buffButton1Text.color = normalTextColor;
        if (buffButton2Text != null) buffButton2Text.color = normalTextColor;
        if (buffButton3Text != null) buffButton3Text.color = normalTextColor;

        // 高亮选中的按钮
        switch (optionIndex)
        {
            case 0:
                if (buffButton1Text != null) buffButton1Text.color = selectedTextColor;
                break;
            case 1:
                if (buffButton2Text != null) buffButton2Text.color = selectedTextColor;
                break;
            case 2:
                if (buffButton3Text != null) buffButton3Text.color = selectedTextColor;
                break;
        }
    }
}
