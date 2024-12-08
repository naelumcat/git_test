using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using System;
using Spine;

public abstract class Note : MonoBehaviour, INote
{
    protected Mediator mediator => Mediator.i;

    public delegate void OnEndInteractDelegate(Note note, NoteResult result);
    public OnEndInteractDelegate OnEndInteractCallback;

    [System.Serializable]
    public struct PlaceableDesc
    {
        public bool Air;
        public bool Center;
        public bool Road;

        public static PlaceableDesc Default()
        {
            return new PlaceableDesc()
            {
                Air = true,
                Center = false,
                Road = true,
            };
        }
        public static PlaceableDesc CenterOnly()
        {
            return new PlaceableDesc()
            {
                Air = false,
                Center = true,
                Road = false,
            };
        }
    }

    [System.Serializable]
    public class CustomOverrideHitEffectDesc
    {
        public MapType mapType = MapType._01;
        public Effect airEffect = null;
        public Effect centerEffect = null;
        public Effect roadEffect = null;
    }

    public GameObject body = null;

    [Header("Note")]
    public NoteData data = NoteData.Default();

    [SerializeField, ReadOnly]
    protected PlaceableDesc placeableDesc = PlaceableDesc.Default();

    protected MapType mapType { get; private set; } = MapType._01;

    MeshRenderer airSpineMeshRenderer = null;
    MeshRenderer centerSpineMeshRenderer = null;
    MeshRenderer roadSpineMeshRenderer = null;

    float noteRatio = 0.0f;

    [Header("Spine")]

    [SerializeField]
    SkeletonAnimation airSpineComp = null;

    [SerializeField]
    SkeletonAnimation centerSpineComp = null;

    [SerializeField]
    SkeletonAnimation roadSpineComp = null;

    [SerializeField]
    List<Spine.Unity.SkeletonDataAsset> airSkeletonDatas;

    [SerializeField]
    List<Spine.Unity.SkeletonDataAsset> centerSkeletonDatas;

    [SerializeField]
    List<Spine.Unity.SkeletonDataAsset> roadSkeletonDatas;

    [SerializeField]
    List<string> skins;

    [Header("Hit Effect")]

    [SerializeField]
    Effect defaultHitEffect = null;

    [SerializeField]
    Effect feverHitEffect = null;

    [SerializeField]
    List<CustomOverrideHitEffectDesc> customOverrideHitEffects;

    Dictionary<MapType, CustomOverrideHitEffectDesc> customOverrideHitEffectDescs = new Dictionary<MapType, CustomOverrideHitEffectDesc>();

    [SerializeField]
    bool useCustomSpawnEffects;

    [Header("Score")]
    public float defaultScore = 0;
    public float subScore = 0;

    bool selected = false;

    // ���� �Ǵ� �ν��Ͻ� �� �� Init �Լ��� ȣ���ϸ� Ȱ��ȭ�˴ϴ�.
    public bool isInstance { get; private set; }

    List<NoteComponent> noteComponents = null;
    NoteCollider noteColliderComponent = null;
    NoteSortingOrder sortingOrderComponent = null;
    NoteColor noteColorComponent = null;
    NoteSubHandle subHandleComponent = null;

    bool dead = false;

    bool hitable = true;

    public SkeletonAnimation airSpine => airSpineComp;
    public SkeletonAnimation centerSpine => centerSpineComp;
    public SkeletonAnimation roadSpine => roadSpineComp;
    public List<Spine.Unity.SkeletonDataAsset> airSpineDatas => airSkeletonDatas;
    public List<Spine.Unity.SkeletonDataAsset> centerSpineDatas => centerSkeletonDatas;
    public List<Spine.Unity.SkeletonDataAsset> roadSpineDatas => roadSkeletonDatas;
    public float ratio => noteRatio;
    public bool isSelected => selected;
    public NoteSortingOrder sortingOrder => sortingOrderComponent;
    public int interactWeight => GetInteratWeight();

    public bool isDead
    {
        get => dead;
        protected set
        {
            dead = value;
            this.gameObject.SetActive(!value);
        }
    }

    public bool isHitable => hitable;

    public abstract NoteType GetNoteType();
    public abstract NoteVisualizeMethod GetNoteVisualizeMethod();
    public abstract SkeletonDataAsset GetNoteVisualizeSpineDataAtTime(float playingTime);
    public abstract Sprite GetNoteVisualizeSpriteDataAtTime(float playingTime);
    public virtual Color GetNoteVisualizeSpriteColor() { return Color.white; }
    public virtual void OnDestroyInstance() { }
    public virtual void Init_Load() { }
    public virtual void Init_CopyInEditor(Note source)
    {
        // ������ ��Ʈ�� ������Ʈ�� ������ �����մϴ�.
        noteComponents?.Clear();
        NoteComponent[] copiedNoteComponents = GetComponentsInChildren<NoteComponent>(true);
        Array.ForEach(copiedNoteComponents, (x) =>
        {
            Destroy(x);
        });
    }
    public virtual void Init_PasteInEditor(Note source)
    {

    }
    public virtual void Init_CreateInEditor(bool isLoaded)
    {
        if (!isLoaded)
        {
            SetupHash(mediator.music);
        }

        // �ش� ������ ���Ѿ� �մϴ�.
        subHandleComponent = AddNoteComponent<NoteSubHandle>();
        noteColorComponent = AddNoteComponent<NoteColor>();
        noteColliderComponent = AddNoteComponent<NoteCollider>();
    }
    public virtual void Init_CreateInGame()
    {

    }
    public virtual void Init()
    {
        isInstance = true;

        ApplyHeight();
        ApplySkeleton();
        ApplyCustomOverrideHitEffectDescs();

        sortingOrderComponent = AddNoteComponent<NoteSortingOrder>();
    }
    protected virtual void OnChangeMapType(MapType value) { }
    protected virtual void UpdateLocalPosition() { }
    protected virtual void UpdateAnimation() { }
    protected virtual void UpdateBossAnimationData() { }
    protected virtual void NoteUpdate() { }
    protected virtual NoteVisibleState GetNoteVisibleStateAtCurrentTime()
    {
        float noteLocalX = noteRatio * mediator.gameSettings.lengthPerSeconds;
        if (noteLocalX < mediator.gameSettings.localLeftVisibleX)
        {   // ��Ʈ�� ���� ���� ����� �ۿ� ������
            return NoteVisibleState.OutsideLeft;
        }
        else if (noteLocalX > mediator.gameSettings.localRightVisibleX)
        {   // ��Ʈ�� ������ ���� ����� �ۿ� ������
            return NoteVisibleState.OutsideRight;
        }
        else
        {   // ��Ʈ�� ���� ��� ���ο� ���Ե�
            return NoteVisibleState.In;
        }
    }
    protected virtual void Spine_Update() { }
    protected virtual NoteResult OnHit(NotePosition notePosition)
    {
        // �̹� �ı��� ��Ʈ�� �����մϴ�.
        if (isDead)
        {
            return NoteResult.None();
        }
        // �ٸ� ������ Ÿ���ϴ� ��� �����մϴ�.
        if (notePosition != data.position)
        {
            return NoteResult.None();
        }
        // ��Ʈ�� �������� �����ϱ���� ���� �ð��� ����մϴ�.
        float diff = Mathf.Abs(mediator.music.adjustedTime - data.time);
        // �������� �����ϱ���� ���� �ð��� �׷���Ʈ ���� �ʰ��̸� �����մϴ�.
        if (diff > mediator.gameSettings.greatDiffTime)
        {
            return NoteResult.None();
        }
        NoteResult result = NoteResult.Hit(notePosition);
        // �������� �����ϱ���� ���� �ð��� ���� ����Ʈ, �׷���Ʈ ������ �����մϴ�.
        result.precision = diff <= mediator.gameSettings.perfectDiffTime ? 
            ComboPrecision.Perfect : ComboPrecision.Great;
        // ������ ���� ����Ʈ�� �����մϴ�.
        SpawnDyingEffect(notePosition, result.precision);
        isDead = true;
        return result;
    }
    protected virtual void OnReleaseHit(NotePosition notePosition) { }
    protected virtual void CustomSpawnDyingEffectPosition(NotePosition notePosition, ref Vector2 worldPosition) { }
    protected virtual NoteResult OnInteract(NotePosition notePosition) { return NoteResult.None(); }
    protected virtual int GetInteratWeight() { return 0; }
    protected virtual void CustomSpawnHitEffectPosition(ref NotePosition notePosition) { }
    protected virtual void OnSpawnHitEffect(NotePosition notePosition, Effect effect) { }
    protected virtual bool CustomDamagableState() { return true; }
    protected virtual bool IsDamagableCharacterPosition(NotePosition characterPosition)
    {
        return characterPosition == data.position;
    }
    protected virtual bool CheckEndHitable()
    {
        float characterTime = mediator.character.timeOfCharacterPos;
        if (characterTime > data.time + mediator.gameSettings.damagableRangeTime)
        {
            return true;
        }
        return false;
    }
    protected virtual void OnEndHitable() { }
    public virtual int GetAccuracyScore() { return 1; }
    public virtual int GetMissCount() { return 1; }
    public virtual int GetMaxCombo() { return 1; }

    protected virtual void OnValidate() { data.type = GetNoteType(); }

    protected virtual void Awake()
    {
        gameObject.tag = Tags.Note;

        if (airSpineComp)
        {
            airSpineMeshRenderer = airSpineComp.GetComponent<MeshRenderer>();
            airSpineComp.UpdateLocal += delegate { Spine_Update(); };
            airSpineComp.OnRebuild += delegate { Spine_Update(); };
        }
        if (centerSpine)
        {
            centerSpineMeshRenderer = centerSpineComp.GetComponent<MeshRenderer>();
            centerSpineComp.UpdateLocal += delegate { Spine_Update(); };
            centerSpineComp.OnRebuild += delegate { Spine_Update(); };
        }
        if (roadSpine)
        {
            roadSpineMeshRenderer = roadSpineComp.GetComponent<MeshRenderer>();
            roadSpineComp.UpdateLocal += delegate { Spine_Update(); };
            roadSpineComp.OnRebuild += delegate { Spine_Update(); };
        }

        if (noteComponents == null)
        {
            noteComponents = new List<NoteComponent>();
        }
    }

    public void SetNoteActive(bool value)
    {
        GameObject activeObject = null;

        if (mediator.gameSettings.isEditor)
        {
            activeObject = body;
        }
        else
        {
            activeObject = this.gameObject;
        }

        if (value)
        {
            if (!activeObject.activeSelf)
            {
                activeObject.SetActive(true);
                this.enabled = true;
                if (sortingOrderComponent) sortingOrderComponent.enabled = true;
                if (noteColorComponent) noteColorComponent.enabled = true;
                if (noteColliderComponent) noteColliderComponent.enabled = true;
            }
        }
        else
        {
            if (activeObject.activeSelf)
            {
                activeObject.SetActive(false);
                this.enabled = false;
                if (sortingOrderComponent) sortingOrderComponent.enabled = false;
                if (noteColorComponent) noteColorComponent.enabled = false;
                if (noteColliderComponent) noteColliderComponent.enabled = false;

                // ��Ȱ��ȭ ���� �� �� ȣ�����ݴϴ�.
                // �ʴ� ������ ���� ������ �ĸ����� ���ϴ� ��츦 �����մϴ�.
                UpdateMapType();
                UpdateLocalPosition();
                UpdateAnimation();
            }
        }
    }

    private T AddNoteComponent<T>() where T : NoteComponent
    {
        if (noteComponents == null)
        {
            noteComponents = new List<NoteComponent>();
        }

        T[] components = gameObject.GetComponentsInChildren<T>(true);
        if (components.Length > 0)
        {
            Array.ForEach(components, (x) => DestroyImmediate(x));
            noteComponents.RemoveAll((x) => x is T);
        }

        T noteComponent = gameObject.AddComponent<T>();
        noteComponent.Init(this);

        noteComponents.Add(noteComponent);

        return noteComponent;
    }

    public NoteVisibleState UpdateNote()
    {
        if (isDead)
        {
            return NoteVisibleState.OutsideRight;
        }
        UpdateRatio(); // ��Ʈ�� ������ ������ �Ÿ��� ������ ���
        // ��Ʈ�� ȭ�� �ȿ� �ִ��� �˻�
        NoteVisibleState visibleState = GetNoteVisibleStateAtCurrentTime();
        switch (visibleState)
        {
            case NoteVisibleState.In: // ��Ʈ�� ȭ�� �ȿ� �ִ� ���
            {
                SetNoteActive(true); // ��Ʈ Ȱ��ȭ
                UpdateMapType(); // �� ������ ���� ��Ų ����
                UpdateLocalPosition(); // ��Ʈ�� ��ġ ����
                UpdateAnimation(); // ��Ʈ�� ������ �ִϸ��̼� ����
                NoteUpdate(); // ��Ʈ�� �߰� �۾�
                if (!mediator.gameSettings.isEditor)
                {
                    TakeDamage();
                    UpdateHitableState();
                }
            }
            break;
            case NoteVisibleState.OutsideLeft: // ��Ʈ�� ȭ�� �ۿ� �ִ� ���
            case NoteVisibleState.OutsideRight:
            {
                SetNoteActive(false);
            }
            break;
        }
        if (mediator.gameSettings.isEditor)
        {
            UpdateBossAnimationData();
        }
        return visibleState;
    }

    public void SetupHash(Music music)
    {
        data.hash = music.GetNextNoteHash();
    }

    public void UpdateRatio()
    {
        // data.time: ��Ʈ�� �������� ��� �ð�
        // data.speed: ��Ʈ ������ ���
        noteRatio = mediator.music.TimeToRatioAtAdjustedTime(data.time, data.speed);
    }

    /// <summary>
    /// �� Ÿ���� ����Ǿ����� True�� ��ȯ�մϴ�.
    /// </summary>
    /// <returns></returns>
    public bool UpdateMapType()
    {
        MapType newMapType = mediator.music.GetMapTypeAtTime(mediator.music.adjustedTime);
        if (newMapType != mapType)
        {
            mapType = newMapType;
            Init();
            OnChangeMapType(mapType);
            return true;
        }
        return false;
    }

    protected void ApplyHeight()
    {
        switch (data.position)
        {
            case NotePosition.Air:
            {
                airSpineComp?.gameObject.SetActive(true);
                centerSpineComp?.gameObject.SetActive(false);
                roadSpineComp?.gameObject.SetActive(false);
            }
            break;
            case NotePosition.Center:
            {
                airSpineComp?.gameObject.SetActive(true);
                centerSpineComp?.gameObject.SetActive(true);
                roadSpineComp?.gameObject.SetActive(true);
            }
            break;
            case NotePosition.Road:
            {
                airSpineComp?.gameObject.SetActive(false);
                centerSpineComp?.gameObject.SetActive(false);
                roadSpineComp?.gameObject.SetActive(true);
            }
            break;
        }
    }

    protected void ApplySkeleton()
    {
        if (airSpineComp)
        {
            airSpineComp.Init(airSkeletonDatas[(int)mapType], skins[(int)mapType]);
        }
        if (centerSpineComp)
        {
            centerSpineComp.Init(centerSkeletonDatas[(int)mapType], skins[(int)mapType]);
        }
        if (roadSpineComp)
        {
            roadSpineComp.Init(roadSkeletonDatas[(int)mapType], skins[(int)mapType]);
        }
    }

    protected void ClearState()
    {
        airSpineComp?.ClearState();
        centerSpineComp?.ClearState();
        roadSpineComp?.ClearState();
    }

    public float GetHitLocalY()
    {
        float y = 0.0f;
        switch (data.position)
        {
            case NotePosition.Air:
            y = mediator.hitPoint.airLocalPos.y;
            break;

            case NotePosition.Center:
            y = 0.0f;
            break;

            case NotePosition.Road:
            y = mediator.hitPoint.roadLocalPos.y;
            break;
        }
        return y;
    }

    public bool IsPlaceable(NotePosition position)
    {
        switch (position)
        {
            case NotePosition.Air:
            return placeableDesc.Air;

            case NotePosition.Center:
            return placeableDesc.Center;

            case NotePosition.Road:
            return placeableDesc.Road;
        }
        return false;
    }

    public string GetSkinAtTime(float playingTime)
    {
        MapType mapType = mediator.music.GetMapTypeAtTime(playingTime);
        return skins[(int)mapType];
    }

    public void SetTime(float time)
    {
        data.time = time;
    }

    public float GetTime()
    {
        return data.time;
    }

    public void SetSelect(bool selection)
    {
        if (!noteColorComponent)
        {
            noteColorComponent = AddNoteComponent<NoteColor>();
        }

        Color color = selection ? Color.red : Color.white;
        noteColorComponent.SetColor(color);

        if (subHandleComponent)
        {
            Color subHandleColor = selection ? new Color(1, 0, 0, 2) : Color.white;
            subHandleComponent.subColor = subHandleColor;
        }

        selected = selection;
    }

    public void SwapY()
    {
        bool swapped = false;
        switch (data.position)
        {
            case NotePosition.Air:
            data.position = NotePosition.Road;
            swapped = true;
            break;

            case NotePosition.Road:
            data.position = NotePosition.Air;
            swapped = true;
            break;
        }

        if (swapped)
        {
            Init();
            UpdateNote();
        }
    }

    public void Shift(BeatType beatType, HDirection direction)
    {
        List<float> beats = mediator.music.GetBeats(beatType);
        int closetIndex = mediator.music.GetClosetBeatIndexAtTime(data.time, beatType);
        int nextIndex = closetIndex + (int)direction;

        if (nextIndex == Mathf.Clamp(nextIndex, 0, beats.Count - 1))
        {
            // �ش� �������� �̵� �����ϸ� �̵���ŵ�ϴ�.
            float deltaTime = data.time - beats[closetIndex];
            data.time = beats[nextIndex] + deltaTime;
        }
    }

    public void FitToBeat(BeatType beatType, HDirection direction)
    {
        List<float> beats = mediator.music.GetBeats(beatType);
        int index = mediator.music.GetBeatIndexToShift(data.time, mediator.noteViewport.beatType, direction);
        data.time = beats[index];
    }

    public void FitToClosetBeat(BeatType beatType)
    {
        List<float> beats = mediator.music.GetBeats(beatType);
        int index = mediator.music.GetNearBeatIndex(data.time, beatType);
        data.time = beats[index];
    }

    protected void ApplyAnimation(SkeletonAnimation skeletonAnimation, Spine.Animation animation, float time, bool loop)
    {
        if (!skeletonAnimation.isActiveAndEnabled)
        {
            return;
        }

        skeletonAnimation.ApplyAnimation(animation, time, loop);
    }

    protected void ApplyCustomOverrideHitEffectDescs()
    {
        customOverrideHitEffectDescs.Clear();
        foreach (CustomOverrideHitEffectDesc desc in customOverrideHitEffects)
        {
            customOverrideHitEffectDescs.Add(desc.mapType, desc);
        }
    }

    protected Effect SpawnHitEffect(NotePosition notePosition)
    {
        CustomSpawnHitEffectPosition(ref notePosition);
        Effect effect = mediator.gameState.isFever ? feverHitEffect : defaultHitEffect;

        CustomOverrideHitEffectDesc desc = null;
        if (customOverrideHitEffectDescs.TryGetValue(mapType, out desc))
        {
            Effect overridedEffect = null;
            switch (notePosition)
            {
                case NotePosition.Air:
                overridedEffect = desc.airEffect;
                break;

                case NotePosition.Center:
                overridedEffect = desc.centerEffect;
                break;

                case NotePosition.Road:
                overridedEffect = desc.roadEffect;
                break;
            }

            if (overridedEffect != null)
            {
                effect = overridedEffect;
            }
        }

        return SpawnHitEffect_Internal(notePosition, effect);
    }

    // �����ǿ� �ش��ϴ� Ű�� �������� ȣ���մϴ�.
    public NoteResult Hit(NotePosition notePosition)
    {
        if (!hitable)
        {
            return NoteResult.None();
        }

        NoteResult result = OnHit(notePosition);
        if (result.hit)
        {
            Effect effect = SpawnHitEffect(notePosition);
            OnSpawnHitEffect(notePosition, effect);
        }
        return result;
    }

    // ������ �����ǿ� �ش��ϴ� �� Ű �̻��� ���� �־��� ���� ������ �����ǿ� �ش��ϴ� ��� Ű�� ���������� �� �Լ��� ȣ���մϴ�.
    public void ReleaseHit(NotePosition notePosition)
    {
        OnReleaseHit(notePosition);
    }

    public NoteResult Interact(NotePosition notePosition)
    {
        if (!hitable)
        {
            return NoteResult.None();
        }

        NoteResult result = OnInteract(notePosition);
        if (result.hit)
        {
            Effect effect = SpawnHitEffect(notePosition);
            OnSpawnHitEffect(notePosition, effect);
        }
        return result;
    }

    protected void SpawnDyingEffect(NotePosition notePosition, ComboPrecision type)
    {
        SkeletonAnimation skeleton = null;
        float worldY = 0;
        switch (notePosition)
        {
            case NotePosition.Air:
            skeleton = airSpine;
            worldY = mediator.hitPoint.airPos.y;
            break;

            case NotePosition.Center:
            skeleton = centerSpine;
            worldY = mediator.hitPoint.Pos.y;
            break;

            case NotePosition.Road:
            skeleton = roadSpine;
            worldY = mediator.hitPoint.roadPos.y;
            break;
        }

        if (!skeleton)
        {
            return;
        }

        string animationName;
        switch (type)
        {
            case ComboPrecision.Great:
            animationName = "out_g";
            break;

            case ComboPrecision.Perfect:
            animationName = "out_p";
            break;

            default:
            animationName = "";
            break;
        }

        SpineEffect.SetupDesc desc = SpineEffect.SetupDesc.Default();
        desc.skin = skeleton.initialSkinName;

        MeshRenderer meshRenderer = null;
        if (!skeleton.TryGetComponent<MeshRenderer>(out meshRenderer))
        {
            return;
        }
        desc.sortingLayer = meshRenderer.sortingLayerName;
        desc.sortingOrder = meshRenderer.sortingOrder;

        SpineEffect effect = mediator.spineEffectPool.SpawnEffectToFixedUpdate(skeleton.skeletonDataAsset, animationName, desc, mediator.music.adjustedTime, () => mediator.music.adjustedTime);
        float hitPointSpaceLocalX = ratio * mediator.gameSettings.lengthPerSeconds;
        float worldX = mediator.hitPoint.transform.TransformPoint(hitPointSpaceLocalX, 0, 0).x;

        Vector2 worldPosition = new Vector2(worldX, worldY);
        CustomSpawnDyingEffectPosition(notePosition, ref worldPosition);
        effect.transform.position = worldPosition;
    }

    protected Effect SpawnHitEffect_Internal(NotePosition notePosition, Effect prefab)
    {
        float worldY = 0;
        switch (notePosition)
        {
            case NotePosition.Air:
            worldY = mediator.hitPoint.airPos.y;
            break;

            case NotePosition.Center:
            worldY = mediator.hitPoint.Pos.y;
            break;

            case NotePosition.Road:
            worldY = mediator.hitPoint.roadPos.y;
            break;
        }

        Effect effect = mediator.effectPool.SpawnEffect(prefab);

        float worldX = mediator.hitPoint.Pos.x;
        Vector2 worldPosition = new Vector2(worldX, worldY);
        effect.transform.position = worldPosition;
        return effect;
    }

    public bool IsOnFrontOfCharacter()
    {
        float characterTime = mediator.character.timeOfCharacterPos;
        return characterTime < data.time;
    }

    public bool IsOnDamagableRange()
    {
        float characterTime = mediator.character.timeOfCharacterPos;
        float diffTime = characterTime - data.time;

        // ��Ʈ�� ĳ���� �������κ��� �ڿ� ��ġ.
        // �׸��� ĳ���Ϳ� ��Ʈ ������ �ð��� ������ �ð� �̳�.
        return diffTime >= 0 && diffTime < mediator.gameSettings.damagableRangeTime;
    }

    public bool IsOnSimplteNoteInteractibleRange()
    {
        float characterTime = mediator.character.timeOfCharacterPos;
        float absDiffTime = Mathf.Abs(characterTime - data.time);
        // ��Ʈ�� ������ �ڿ� ��ġ. �׸��� ��Ʈ�� ĳ������ �տ� ��ġ.
        // Ȥ�� ĳ���Ϳ� ��Ʈ ������ �ð��� ������ �ð� �̳�.
        return 
            (data.time <= mediator.music.adjustedTime && data.time >= characterTime) || 
            absDiffTime < mediator.gameSettings.simpleNoteInteractibleRangeTime;
    }

    public bool IsOnGearWehalNotePassRange()
    {
        float characterTime = mediator.character.timeOfCharacterPos;
        float diffTime = characterTime - data.time;

        // ��Ʈ�� ĳ������ �ڿ� ��ġ.
        // ��Ʈ�� ������ ������ ���.
        return diffTime > mediator.gameSettings.damagableRangeTime;
    }

    protected void TakeDamage()
    {
        if (!hitable)
        {
            return;
        }

        if (!IsOnDamagableRange())
        {
            return;
        }

        NotePosition characterPosition = mediator.character.characterPosition;
        if (!IsDamagableCharacterPosition(characterPosition))
        {
            return;
        }

        if (!CustomDamagableState())
        {
            return;
        }

        hitable = false;
        mediator.character.TakeDamage();
        mediator.gameState.Miss(this);
    }

    protected void UpdateHitableState()
    {
        if (!hitable)
        {
            return;
        }

        if (CheckEndHitable())
        {
            hitable = false;
            mediator.gameState.Miss(this);
            OnEndHitable();
        }
    }

    protected void SetToNonHitable()
    {
        hitable = false;
    }
}
