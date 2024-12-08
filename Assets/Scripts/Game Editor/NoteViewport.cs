using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum SelectMouseType
{
    MouseDown,
    MouseUp,
}

public class NoteViewport : MonoBehaviour, IPointerClickHandler, IPointerExitHandler, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
{
    protected Mediator mediator => Mediator.i;

    struct VisibleBeat
    {
        public int index;
        public float localCenterX;
        public float localLeftX;
        public float localRightX;
        public float localUpYTop;
        public float localUpYBot;
        public float localDownYTop;
        public float localDownYBot;
    }

    // ���õ� ��Ʈ�� �ð��� ���� ��ġ�� ���޵˴ϴ�.
    public delegate void OnSelectBeatDelegate(float time, NotePosition position);

    // ���õ� ��Ʈ��� � �Է¿� ���� ���õǾ������� ���޵˴ϴ�.
    // ���� ù ��°�� ���õ� ��Ʈ �ݶ��̴��� ��Ʈ ��� �ڵ��� �����Ǿ� �ִٸ� �ش� ������Ʈ ���� ���޵˴ϴ�.
    public delegate void OnSelectNotesDelegate(List<Note> notes, NoteElementHandle elementHandle, SelectMouseType eventType);

    // �ƹ� �͵� ���õ��� ���� ��쿡 ȣ��˴ϴ�.
    public delegate void OnSelectEmptyDelegate(SelectMouseType eventType);

    public event OnSelectBeatDelegate OnSelectBeat;
    public event OnSelectNotesDelegate OnSelectNotes;
    public event OnSelectEmptyDelegate OnSelectEmpty;

    public float localGridHeight = 1.0f;
    public float localGridWidth = 0.3f;
    public BeatType beatType = BeatType.B4;

    public bool enableDrag = false;

    RectTransform rectTransform = null;
    Material glMaterial = null;
    Material glMaterialNonMasking = null;
    List<VisibleBeat> visibleBeats;

    UnityEngine.UI.Image emptyImage = null;
    bool pointerHovering = false; // ���콺�� �� �г� ���� �ֽ��ϴ�.
    bool mouseDrag = false; // �� �гο��� ���콺�� Ŭ���Ǿ���, ���� ���콺�� ���� �ʾҽ��ϴ�.
    float mouseDownTime; // ���콺�� Ŭ�� ������ ��� �ð��Դϴ�.
    float mouseDownWorldY; // ���콺�� Ŭ�� ������ ����y �Դϴ�.

    bool overlappedBeat = false; // �������� ��Ʈ�� �ֽ��ϴ�.
    int reactBeatIndex = -1; // �������� ��Ʈ�� ������ -1�Դϴ�.
    NotePosition reactBeatPosition = 0;

    public void IncreaseBeat()
    {
        beatType = Utility.LoopEnum(beatType);
    }

    public bool ClosetBeatOnScreen(Vector2 screenPosition, out int closetBeatIndex, out NotePosition closetBeatPosition)
    {
        Vector2 worldMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return ClosetBeatOnWorld(worldMouse, out closetBeatIndex, out closetBeatPosition);
    }

    public bool ClosetBeatOnWorld(Vector2 worldPosition, out int closetBeatIndex, out NotePosition closetBeatPosition)
    {
        closetBeatIndex = -1;
        closetBeatPosition = 0;
        Vector2 localPosition = rectTransform.InverseTransformPoint(worldPosition);
        int closetIndex = -1; // x��ġ�� ����� ��Ʈ �ε���
        float closetDistance = float.MaxValue;
        for (int i = 0; i < visibleBeats.Count; ++i)
        {
            float distance = Mathf.Abs(localPosition.x - visibleBeats[i].localCenterX);
            if (distance < closetDistance)
            {
                closetDistance = distance;
                closetIndex = i;
            }
        }
        if (closetIndex != -1)
        {
            VisibleBeat closetBeat = visibleBeats[closetIndex];
            float upY = (closetBeat.localUpYTop + closetBeat.localUpYBot) * 0.5f;
            float centerY = (closetBeat.localUpYBot + closetBeat.localDownYTop) * 0.5f;
            float downY = (closetBeat.localDownYTop + closetBeat.localDownYBot) * 0.5f;
            float[] yPos = { upY, centerY, downY };
            NotePosition[] notePos = { NotePosition.Air, NotePosition.Center, NotePosition.Road };
            float minYDistance = float.MaxValue;
            NotePosition minYDistancePos = NotePosition.Air;
            for(int i = 0; i < 3; ++i)
            {
                float yDistance = Mathf.Abs(localPosition.y - yPos[i]);
                if(yDistance < minYDistance)
                {
                    minYDistance = yDistance;
                    minYDistancePos = notePos[i];
                }
            }
            closetBeatIndex = visibleBeats[closetIndex].index;
            closetBeatPosition = minYDistancePos;
            return true;
        }
        return false;
    }

    public bool OverlapBeatOnScreen(Vector2 screenPosition, out int overlapedBeatIndex, out NotePosition overlapedPosition)
    {
        Vector2 worldMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return OverlapBeatOnWorld(worldMouse, out overlapedBeatIndex, out overlapedPosition);
    }

    public bool OverlapBeatOnWorld(Vector2 worldPosition, out int overlapedBeatIndex, out NotePosition overlapedPosition)
    {
        overlapedBeatIndex = -1;
        overlapedPosition = 0;
        Vector2 localPosition = rectTransform.InverseTransformPoint(worldPosition);
        int closetIndex = -1; // x��ġ�� ����� ��Ʈ �ε���
        float closetDistance = float.MaxValue;
        for (int i = 0; i < visibleBeats.Count; ++i)
        {
            float distance = Mathf.Abs(localPosition.x - visibleBeats[i].localCenterX);
            if (distance < closetDistance)
            {
                closetDistance = distance;
                closetIndex = i;
            }
        }
        if (closetIndex != -1)
        {
            VisibleBeat closetBeat = visibleBeats[closetIndex];
            if (localPosition.x > closetBeat.localLeftX && localPosition.x < closetBeat.localRightX)
            {
                if (localPosition.y > closetBeat.localUpYBot && localPosition.y < closetBeat.localUpYTop)
                {
                    overlapedBeatIndex = visibleBeats[closetIndex].index;
                    overlapedPosition = NotePosition.Air;
                    return true;
                }
                else if (localPosition.y > closetBeat.localDownYTop && localPosition.y < closetBeat.localUpYBot)
                {
                    overlapedBeatIndex = visibleBeats[closetIndex].index;
                    overlapedPosition = NotePosition.Center;
                    return true;
                }
                else if (localPosition.y > closetBeat.localDownYBot && localPosition.y < closetBeat.localDownYTop)
                {
                    overlapedBeatIndex = visibleBeats[closetIndex].index;
                    overlapedPosition = NotePosition.Road;
                    return true;
                }
            }
        }
        return false;
    }

    void UpdateVisibleBeats()
    {
        float localUpHitPointTop = rectTransform.InverseTransformPoint(0, mediator.hitPoint.airPos.y + localGridHeight, 0).y;
        float localUpHitPointBot = rectTransform.InverseTransformPoint(0, mediator.hitPoint.airPos.y - localGridHeight, 0).y;
        float localDownHitPointTop = rectTransform.InverseTransformPoint(0, mediator.hitPoint.roadPos.y + localGridHeight, 0).y;
        float localDownHitPointBot = rectTransform.InverseTransformPoint(0, mediator.hitPoint.roadPos.y - localGridHeight, 0).y;
        float localGridWidth = rectTransform.InverseTransformVector(this.localGridWidth, 0, 0).x;
        float panelWidth = rectTransform.rect.width;
        List<float> beats = mediator.music.GetBeats(beatType);

        // ���� Ž���� �ð��� ���մϴ�.
        // �г��� �� ���� ��ġ�� ��� �ð����� ��ȯ�Ͽ� �ش� �ð����� ��Ʈ�� �׸��� �����Դϴ�.
        // ���� �������� �г��� ����x�� Ÿ������ x ������ �Ÿ��� ������ ��ȯ�մϴ�.
        // �ش� ������ �ð����� �ٲٸ� �г��� ����x���� Ÿ���������� �ð��� ���� �� �ֽ��ϴ�.
        // �ش� �ð��� ���� ��� �ð����� ����(�������� �ű��) ��Ʈ �׸����� ���� �ð��� ���� �� �ֽ��ϴ�.
        float worldLeftX = rectTransform.TransformPoint(rectTransform.rect.xMin, 0, 0).x;
        float worldDistX = Mathf.Abs(worldLeftX - mediator.hitPoint.Pos.x);
        float deltaRatio = worldDistX / mediator.gameSettings.lengthPerSeconds;
        float deltaTime = MusicUtility.RatioToTime(deltaRatio, mediator.music.currentGlobalSpeedScale, 0);
        float searchBeginTime = mediator.music.playingTime - deltaTime;  
        int closetIndex = mediator.music.GetClosetBeatIndexAtTime(searchBeginTime, beatType);

        visibleBeats.Clear();
        for (int i = closetIndex; i < beats.Count; ++i)
        {
            // �������� ��ġ�ϵ��� ��Ʈ ��ġ�� ���մϴ�.
            // ���� ��Ʈ�� ��ġ�ϴ� ��쿡�� �������� ��Ȯ�� ��ġ�ؾ� �ϹǷ� ��Ʈ�� �������� x��ġ�� ratio�� 0�϶� ��ġ�ϵ��� �մϴ�.
            float ratio = mediator.music.TimeToRatioAtCurrentTime(beats[i], 1.0f);
            float worldX = mediator.hitPoint.Pos.x + (float)ratio * mediator.gameSettings.lengthPerSeconds;
            float localX = rectTransform.InverseTransformPoint(worldX, 0, 0).x;
            VisibleBeat visibleBeat = new VisibleBeat();
            visibleBeat.index = i;
            visibleBeat.localCenterX = localX;
            visibleBeat.localLeftX = localX - localGridWidth;
            visibleBeat.localRightX = localX + localGridWidth;
            visibleBeat.localUpYTop = localUpHitPointTop;
            visibleBeat.localUpYBot = localUpHitPointBot;
            visibleBeat.localDownYTop = localDownHitPointTop;
            visibleBeat.localDownYBot = localDownHitPointBot;
            if (visibleBeat.localLeftX > rectTransform.rect.xMax && visibleBeat.localRightX > rectTransform.rect.xMax)
            {
                break;
            }
            visibleBeats.Add(visibleBeat);
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        pointerHovering = true;
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        pointerHovering = false;
        overlappedBeat = false;
    }

    void IPointerClickHandler.OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        mouseDrag = enableDrag;
        Vector2 worldMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 hitPointSpaceLocalMouse = mediator.hitPoint.transform.InverseTransformPoint(worldMouse);
        mouseDownTime = mediator.music.LocalXToTime(hitPointSpaceLocalMouse.x, mediator.music.playingTime);
        mouseDownWorldY = worldMouse.y;

        if (overlappedBeat)
        {
            float time = mediator.music.GetBeats(beatType)[reactBeatIndex];
            NotePosition position = reactBeatPosition;
            OnSelectBeat?.Invoke(time, position);

            mouseDrag = false;
        }   

        {
            NoteElementHandle elementHandle;
            Note note = NoteCollider.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), out elementHandle);
            if (note)
            {
                List<Note> notes = new List<Note>();
                notes.Add(note);
                OnSelectNotes?.Invoke(notes, elementHandle, SelectMouseType.MouseDown);

                mouseDrag = false;
            }
        }
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        if (mouseDrag)
        {
            float hitPointLocalFirstMouseX = mediator.music.TimeToLocalXAtCurrentTime(mouseDownTime, 1);
            float worldFirstMouseX = mediator.hitPoint.transform.TransformPoint(hitPointLocalFirstMouseX, 0, 0).x;
            Vector2 worldFirstMouse = new Vector2(worldFirstMouseX, mouseDownWorldY);
            Vector2 worldMouse = ClampedWorldMousePos(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            List<Note> overlaped = NoteCollider.OverlapRect(worldFirstMouse, worldMouse);

            if(overlaped.Count > 0)
            {
                OnSelectNotes?.Invoke(overlaped, null, SelectMouseType.MouseUp);
            }
            else
            {
                OnSelectEmpty?.Invoke(SelectMouseType.MouseUp);
            }
        }

        mouseDrag = false;
    }

    Vector2 ClampedWorldMousePos(Vector2 worldMouse)
    {
        Vector2 worldHalfExtents = Camera.main.OrthographicExtents() * 0.5f;
        Vector2 camPos = Camera.main.transform.position;
        worldMouse.x = Mathf.Clamp(worldMouse.x, camPos.x - worldHalfExtents.x, camPos.x + worldHalfExtents.x);
        worldMouse.y = Mathf.Clamp(worldMouse.y, camPos.y - worldHalfExtents.y, camPos.y + worldHalfExtents.y);
        return worldMouse;
    }

    void MouseScroll()
    {
        const float scrollTick = 0.1f;
        float deltaY = Input.mouseScrollDelta.y;
        if (Mathf.Abs(deltaY) > 0)
        {
            mediator.music.playingTime += -deltaY * scrollTick;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // ��ǲ �̺�Ʈ�� �ޱ� ���� �̹����� �߰��մϴ�.
        emptyImage = gameObject.GetComponent<UnityEngine.UI.Image>();
        if (emptyImage == null)
        {
            emptyImage = gameObject.AddComponent<UnityEngine.UI.Image>();
            emptyImage.color = Color.clear;
        }

        Shader shader = Shader.Find("GL/GL");

        glMaterial = new Material(shader);
        glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        glMaterial.SetInt("_ZWrite", 0);

        glMaterial.SetInt("_StencilComp", (int)UnityEngine.Rendering.CompareFunction.Equal);
        glMaterial.SetInt("_Stencil", 0);
        glMaterial.SetInt("_StencilOp", (int)UnityEngine.Rendering.StencilOp.Keep);

        glMaterialNonMasking = new Material(shader);
        glMaterialNonMasking.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glMaterialNonMasking.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glMaterialNonMasking.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        glMaterialNonMasking.SetInt("_ZWrite", 0);

        visibleBeats = new List<VisibleBeat>();
    }

    private void Update()
    {
        UpdateVisibleBeats();

        if (pointerHovering)
        {
            overlappedBeat = OverlapBeatOnScreen(Input.mousePosition, out reactBeatIndex, out reactBeatPosition);

            MouseScroll();
        }
    }

    private void OnRenderObject()
    {
        float localUpHitPointTop = rectTransform.InverseTransformPoint(0, mediator.hitPoint.airPos.y + localGridHeight, 0).y;
        float localUpHitPointBot = rectTransform.InverseTransformPoint(0, mediator.hitPoint.airPos.y - localGridHeight, 0).y;
        float localDownHitPointTop = rectTransform.InverseTransformPoint(0, mediator.hitPoint.roadPos.y + localGridHeight, 0).y;
        float localDownHitPointBot = rectTransform.InverseTransformPoint(0, mediator.hitPoint.roadPos.y - localGridHeight, 0).y;
        float localGridWidth = rectTransform.InverseTransformVector(this.localGridWidth, 0, 0).x;

        float panelWidth = rectTransform.rect.width;
        float panelHalfWidth = panelWidth * 0.5f;
        float panelHeight = rectTransform.rect.height;
        float panelHalfHiehgt = panelHeight * 0.5f;

        glMaterial.SetPass(0);

        GL.MultMatrix(transform.localToWorldMatrix);

        // Draw guide line
        GL.Begin(GL.LINES);
        {
            GL.Color(Color.white);

            GL.Vertex3(-panelHalfWidth, localUpHitPointTop, 0);
            GL.Vertex3(+panelHalfWidth, localUpHitPointTop, 0);

            GL.Vertex3(-panelHalfWidth, localUpHitPointBot, 0);
            GL.Vertex3(+panelHalfWidth, localUpHitPointBot, 0);

            GL.Vertex3(-panelHalfWidth, localDownHitPointTop, 0);
            GL.Vertex3(+panelHalfWidth, localDownHitPointTop, 0);

            GL.Vertex3(-panelHalfWidth, localDownHitPointBot, 0);
            GL.Vertex3(+panelHalfWidth, localDownHitPointBot, 0);
        }
        GL.End();

        // Draw beats
        GL.Begin(GL.QUADS);
        {
            foreach(VisibleBeat beat in visibleBeats)
            {
                Color upBeatColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);
                Color centerBeatColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);
                Color downBeatColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);

                if (overlappedBeat && reactBeatIndex == beat.index)
                {
                    if (reactBeatPosition == NotePosition.Air)
                    {
                        upBeatColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }
                    else if (reactBeatPosition == NotePosition.Center)
                    {
                        centerBeatColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }
                    else if (reactBeatPosition == NotePosition.Road) 
                    {
                        downBeatColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }
                }

                GL.Color(upBeatColor);
                GL.Vertex3(beat.localLeftX, beat.localUpYTop, 0);
                GL.Vertex3(beat.localRightX, beat.localUpYTop, 0);
                GL.Vertex3(beat.localRightX, beat.localUpYBot, 0);
                GL.Vertex3(beat.localLeftX, beat.localUpYBot, 0);

                GL.Color(centerBeatColor);
                GL.Vertex3(beat.localLeftX, beat.localUpYBot, 0);
                GL.Vertex3(beat.localRightX, beat.localUpYBot, 0);
                GL.Vertex3(beat.localRightX, beat.localDownYTop, 0);
                GL.Vertex3(beat.localLeftX, beat.localDownYTop, 0);

                GL.Color(downBeatColor);
                GL.Vertex3(beat.localLeftX, beat.localDownYTop, 0);
                GL.Vertex3(beat.localRightX, beat.localDownYTop, 0);
                GL.Vertex3(beat.localRightX, beat.localDownYBot, 0);
                GL.Vertex3(beat.localLeftX, beat.localDownYBot, 0);
            }
        }
        GL.End();

        glMaterialNonMasking.SetPass(0);

        GL.MultMatrix(transform.localToWorldMatrix);

        // Draw selection rect
        if (mouseDrag)
        {
            float hitPointLocalFirstMouseX = mediator.music.TimeToLocalXAtCurrentTime(mouseDownTime, 1);
            float worldFirstMouseX = mediator.hitPoint.transform.TransformPoint(hitPointLocalFirstMouseX, 0, 0).x;
            Vector2 worldFirstMouse = new Vector2(worldFirstMouseX, mouseDownWorldY);
            Vector2 screenMouse = Input.mousePosition;
            Vector2 worldMouse = ClampedWorldMousePos(Camera.main.ScreenToWorldPoint(screenMouse));
            Vector2 localMouseA = rectTransform.InverseTransformPoint(worldFirstMouse);
            Vector2 localMouseB = rectTransform.InverseTransformPoint(worldMouse);
            Vector2 min = new Vector2(Mathf.Min(localMouseA.x, localMouseB.x), Mathf.Min(localMouseA.y, localMouseB.y));
            Vector2 max = new Vector2(Mathf.Max(localMouseA.x, localMouseB.x), Mathf.Max(localMouseA.y, localMouseB.y));

            Color selectionRectFillColor = new Color(0, 1, 0, 0.5f);
            Color selectionRectLineColor = new Color(0, 1, 0, 1);

            GL.Begin(GL.QUADS);
            GL.Color(selectionRectFillColor);
            GL.Vertex3(min.x, max.y, 0);
            GL.Vertex3(max.x, max.y, 0);
            GL.Vertex3(max.x, min.y, 0);
            GL.Vertex3(min.x, min.y, 0);
            GL.End();

            GL.Begin(GL.LINE_STRIP);
            GL.Color(selectionRectLineColor);
            GL.Vertex3(min.x, max.y, 0);
            GL.Vertex3(max.x, max.y, 0);
            GL.Vertex3(max.x, min.y, 0);
            GL.Vertex3(min.x, min.y, 0);
            GL.Vertex3(min.x, max.y, 0);
            GL.End();
        }
    }
}
