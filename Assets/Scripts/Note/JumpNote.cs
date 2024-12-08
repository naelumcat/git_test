using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class JumpNote : Note
{
    [System.Serializable]
    public struct AnimationDesc
    {
        public string air;
        public float airInRatio;
        public float airHitRatio;
        public string road;
        public float roadInRatio;
        public float roadHitRatio;
    }

    [SerializeField]
    List<AnimationDesc> animations;

    Spine.Bone airXBone = null;
    Spine.Bone roadXBone = null;

    Spine.Animation airAnimation = null;
    float airInRatio = 0.0f;
    float airHitRatio = 0.0f;
    Spine.Animation roadAnimation = null;
    float roadInRatio = 0.0f;
    float roadHitRatio = 0.0f;

    public override NoteType GetNoteType()
    {
        return NoteType.Jump;
    }

    public override NoteVisualizeMethod GetNoteVisualizeMethod()
    {
        return NoteVisualizeMethod.Spine;
    }

    public override SkeletonDataAsset GetNoteVisualizeSpineDataAtTime(float playingTime)
    {
        MapType mapType = mediator.music.GetMapTypeAtTime(playingTime);
        return airSpineDatas[(int)mapType];
    }

    public override Sprite GetNoteVisualizeSpriteDataAtTime(float playingTime)
    {
        return null;
    }

    public override void Init()
    {
        base.Init();

        airXBone = airSpine.Skeleton.FindBone("X");
        roadXBone = roadSpine.Skeleton.FindBone("X");

        airAnimation = airSpine.skeleton.Data.FindAnimation(animations[(int)mapType].air);
        roadAnimation = roadSpine.skeleton.Data.FindAnimation(animations[(int)mapType].road);
        airInRatio = animations[(int)mapType].airInRatio;
        airHitRatio = animations[(int)mapType].airHitRatio;
        roadInRatio = animations[(int)mapType].roadInRatio;
        roadHitRatio = animations[(int)mapType].roadHitRatio;

        ClearState();
        SetAnimationByRatio(0);
    }

    protected override void UpdateLocalPosition()
    {
        base.UpdateLocalPosition();

        float y = GetHitLocalY();
        transform.localPosition = new Vector2(ratio * mediator.gameSettings.lengthPerSeconds, y);
    }

    protected override void UpdateAnimation()
    {
        base.UpdateAnimation();

        ClearState();
        SetAnimationByRatio(ratio);
    }

    protected override void Spine_Update()
    {
        airXBone?.SetLocalPosition(Vector2.zero);
        roadXBone?.SetLocalPosition(Vector2.zero);
    }

    private void SetAnimationByRatio(float ratio)
    {
        SetAirAnimationByRatio(ratio);
        SetRoadAnimationByRatio(ratio);
    }

    // invRatio�� ���ؼ� ���� ����ϴ�.
    // 1. invRatio�� ��Ʈ�� ������ beginRatio(���� �ִϸ��̼��� ���۵Ǵ� ��Ʈ�� ����)�϶�
    //      animationRatio�� animationNormalizedBegin(���� �ִϸ��̼��� ���۵Ǵ� �ִϸ��̼��� ����) �̾�� �մϴ�.
    // 2. invRatio�� 1(������) �϶� animationRatio�� animationNormalizedHit(Ÿ������ ���̰� ��ġ�ϰ� �Ǵ� �ִϸ��̼��� ����) �̾�� �մϴ�.
    // ax + b = y : a * ratio + b = animationRatio ��� �� ��
    // 1. a * beginRatio + b = animationNormalizedBegin
    // 2. a * 1 + b = animationNormalizedHit

    // �� ����� �������������� Ǳ�ϴ�.
    // a = (animationNormalizedBegin - animationNormalizedHit) / (beginRatio - 1)
    // b = animationNormalizedHit - a
    // a * invRatio + b = animationRatio

    private void SetAirAnimationByRatio(float ratio)
    {
        float invRatio = 1.0f - ratio;
        float a = (airInRatio - airHitRatio) / (data.jumpNoteData.beginRatio - 1.0f);
        float b = airHitRatio - a;
        float animationRatio = a * invRatio + b;
        animationRatio = Mathf.Clamp(animationRatio, 0.0f, 1.0f);

        ApplyAnimation(airSpine, airAnimation, animationRatio * airAnimation.Duration, false);
    }

    private void SetRoadAnimationByRatio(float ratio)
    {
        float invRatio = 1.0f - ratio;
        float a = (roadInRatio - roadHitRatio) / (data.jumpNoteData.beginRatio - 1.0f);
        float b = roadHitRatio - a;
        float animationRatio = a * invRatio + b;
        animationRatio = Mathf.Clamp(animationRatio, 0.0f, 1.0f);

        ApplyAnimation(roadSpine, roadAnimation, animationRatio * roadAnimation.Duration, false);
    }
}
