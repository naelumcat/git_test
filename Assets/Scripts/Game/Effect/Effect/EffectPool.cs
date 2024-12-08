using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EffectPool : MonoBehaviour, IGameReset
{
    // ���ӿ�����Ʈ �̸����� �з��� ��� ������ ����Ʈ �����Դϴ�.
    Dictionary<string, Stack<Effect>> usableEffects = new Dictionary<string, Stack<Effect>>();
    List<Effect> effects = new List<Effect>();

    private Effect AddEffect(Effect prefab)
    {
        Effect effect = Instantiate<Effect>(prefab);
        // ���ӿ�����Ʈ �̸����� �з��ǹǷ�,
        // ������ ���ӿ�����Ʈ�� �̸��� �ݵ�� �������� �̸��� ���ƾ� �մϴ�.
        effect.gameObject.name = prefab.gameObject.name;
        effect.transform.SetParent(this.transform);
        effect.OnComplete += OnComplete;
        effects.Add(effect);
        return effect;
    }

    private void PushEffect(Effect effect)
    {
        Stack<Effect> stack = null;
        if(!usableEffects.TryGetValue(effect.gameObject.name, out stack))
        {
            stack = new Stack<Effect>();
            usableEffects.Add(effect.gameObject.name, stack);
        }
        stack.Push(effect);
    }

    private Effect PopEffect(Effect prefab)
    {
        Stack<Effect> stack = null;
        if (usableEffects.TryGetValue(prefab.gameObject.name, out stack))
        {
            return stack.Pop();
        }
        return null;
    }

    int GetUsableEffectCount(Effect prefab)
    {
        Stack<Effect> stack = null;
        if (usableEffects.TryGetValue(prefab.gameObject.name, out stack))
        {
            return stack.Count;
        }
        return 0;
    }

    public Effect SpawnEffect(Effect prefab)
    {
        Effect effect = null;
        if (GetUsableEffectCount(prefab) == 0)
        {
            // ��� ������ ����Ʈ�� ���ٸ�
            // �� ����Ʈ�� �����մϴ�.
            effect = AddEffect(prefab);
        }
        else
        {
            // ��� ������ ��Ȱ��ȭ ����Ʈ�� �ִٸ�
            // �ش� ����Ʈ�� ����մϴ�.
            effect = PopEffect(prefab);
            effect.gameObject.SetActive(true);
        }
        effect.PlayEffect();
        return effect;
    }

    public void HideAll()
    {
        foreach (Effect effect in effects)
        {
            effect.ClearState();
        }
    }

    private void OnComplete(Effect effect, Effect.CompleteResult result)
    {
        // ����Ʈ�� �Ϸ�Ǹ� ��Ȱ��ȭ�ϰ�, ���ÿ� ����ϴ�.
        effect.gameObject.SetActive(false);
        PushEffect(effect);
    }

    public void GameReset()
    {
        HideAll();
    }
}
