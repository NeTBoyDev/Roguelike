using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;

[System.Serializable]
public struct AnimationStatePair
{
    [Tooltip("Имя состояния в аниматоре (AnimatorState name)")]
    public string StateName;
    [Tooltip("Новая анимация для замены в этом состоянии")]
    public AnimationClip NewClip;
}

[System.Serializable]
public struct AnimationSet
{
    [Tooltip("Название набора анимаций")]
    public string SetName;
    [Tooltip("Список пар: состояние -> новая анимация")]
    public List<AnimationStatePair> StateAnimations;
}

public class AnimationOverrideManager : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private List<AnimationSet> animationSets = new List<AnimationSet>();

    private AnimatorOverrideController overrideController;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator не найден на объекте!");
                return;
            }
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("Animator Controller не назначен!");
            return;
        }

        // Инициализируем override controller
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;
    }

    /// <summary>
    /// Заменяет анимации в состояниях аниматора на основе указанного набора
    /// </summary>
    /// <param name="setName">Название набора анимаций</param>
    public void ReplaceAnimations(string setName)
    {
        AnimationSet set = animationSets.FirstOrDefault(s => s.SetName == setName);
        if (set.StateAnimations == null || set.StateAnimations.Count == 0)
        {
            Debug.LogWarning($"Набор анимаций '{setName}' не найден или пуст!");
            return;
        }

        Debug.Log($"Применяем набор анимаций: {setName}");

        // Получаем все переопределения из текущего контроллера
        List<KeyValuePair<AnimationClip, AnimationClip>> clipOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(clipOverrides);

        // Создаем словарь для быстрого поиска исходных клипов по именам состояний
        Dictionary<string, AnimationClip> stateToOriginalClip = BuildStateToClipMapping();

        // Применяем новые клипы для каждого состояния из набора
        foreach (var statePair in set.StateAnimations)
        {
            if (string.IsNullOrEmpty(statePair.StateName) || statePair.NewClip == null)
            {
                Debug.LogWarning($"Пропущена пара с пустым именем состояния или клипом в наборе '{setName}'");
                continue;
            }

            if (stateToOriginalClip.TryGetValue(statePair.StateName, out AnimationClip originalClip))
            {
                overrideController[originalClip] = statePair.NewClip;
                Debug.Log($"Заменена анимация для состояния '{statePair.StateName}': {originalClip.name} -> {statePair.NewClip.name}");
            }
            else
            {
                Debug.LogWarning($"Состояние '{statePair.StateName}' не найдено в аниматоре!");
            }
        }

        // Применяем изменения к аниматору
        animator.runtimeAnimatorController = overrideController;
    }

    /// <summary>
    /// Создает словарь, сопоставляющий имена состояний с их исходными клипами
    /// </summary>
    private Dictionary<string, AnimationClip> BuildStateToClipMapping()
    {
        Dictionary<string, AnimationClip> stateToClip = new Dictionary<string, AnimationClip>();

        // Используем AnimatorController для получения структуры состояний
        AnimatorController a = new AnimatorController();
        AnimatorController animatorController = (AnimatorController)animator.runtimeAnimatorController;
        if (animatorController == null)
        {
            Debug.LogError("Не удалось получить AnimatorController!");
            return stateToClip;
        }

        foreach (var layer in animatorController.layers)
        {
            CollectStateClips(layer.stateMachine, stateToClip);
        }

        return stateToClip;
    }

    /// <summary>
    /// Рекурсивно собирает сопоставления имен состояний и их клипов
    /// </summary>
    private void CollectStateClips(UnityEditor.Animations.AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> stateToClip)
    {
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                stateToClip[state.state.name] = clip;
            }
        }

        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            CollectStateClips(subStateMachine.stateMachine, stateToClip);
        }
    }

    // Пример использования в редакторе
#if UNITY_EDITOR
    [ContextMenu("Test Replace Animations")]
    private void TestReplaceAnimations()
    {
        if (animationSets.Count > 0)
        {
            ReplaceAnimations(animationSets[0].SetName);
        }
    }
#endif
}