using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using System;

[DisallowMultipleComponent]
public class NoteSortingOrder : NoteComponent
{
    protected Mediator mediator => Mediator.i;

    List<Renderer> renderers = null;

    protected override void OnInit(Note note)
    {
        Renderer[] rendererComponents = GetComponentsInChildren<Renderer>(true);
        Array.ForEach(rendererComponents, (renderer) => renderer.sortingLayerName = renderer is LineRenderer ? SortingLayers.Line : SortingLayers.Note);
        renderers = new List<Renderer>();
        renderers.AddRange(rendererComponents);
    }

    protected override void OnPostInit()
    {

    }

    private void Update()
    {
        if (!note.enabled)
        {
            return;
        }

        Camera cam = Camera.main;
        Vector2 camPos = cam.transform.position;
        Vector2 camSize = cam.OrthographicExtents();
        float camLeft = camPos.x - camSize.x * 0.5f;

        float hitSpaceNoteLocalX = note.ratio * mediator.gameSettings.lengthPerSeconds;
        float worldNoteX = mediator.hitPoint.transform.TransformPoint(hitSpaceNoteLocalX, 0, 0).x;

        float deltaX = worldNoteX - camLeft;
        float sortingOrder = deltaX * 100.0f;

        for(int i = 0; i < renderers.Count; ++i)
        {
            Renderer renderer = renderers[i];

            // +i�� �������ν� ��Ʈ ���� �������� �������� ���� ��쿡
            // z-fighting�� �����մϴ�.
            renderer.sortingOrder = (int)sortingOrder + i;
        }
    }
}
