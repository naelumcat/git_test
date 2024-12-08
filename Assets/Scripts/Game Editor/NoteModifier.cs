using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoteModifier : MonoBehaviour
{
    protected Mediator mediator => Mediator.i;

    public enum SelectMode
    {
        ReplaceOrDrag,
        Toggle,
        Append,
    }

    struct DragNote
    {
        public float originTime;
        public INote note;
    }

    [SerializeField]
    Button destroyButton = null;

    [SerializeField]
    Button swapYButton = null;

    [SerializeField]
    Button shiftToLeftBeatButton = null;

    [SerializeField]
    Button shiftToRightBeatButton = null;

    [SerializeField]
    Button fitToLeftBeatButton = null;

    [SerializeField]
    Button fitToRightBeatButton = null;

    [SerializeField]
    Button fitToClosetBeatButton = null;

    [SerializeField]
    Button copyButton = null;

    [SerializeField]
    Button pasteToClosetBeatButton = null;

    List<Note> selectedNotes = new List<Note>();
    SelectMode selectMode = SelectMode.ReplaceOrDrag;

    GameObject copiedTempNoteStorageObject = null;
    List<Note> copiedTempNotes = new List<Note>();

    bool dragSelecteNotes = false;
    List<DragNote> dragNotes = new List<DragNote>();
    float dragStartClosetBeatTime = 0;

    void OnClickDestroyButton()
    {
        DestroySelectedNotes();
    }

    void OnClickSwapYButton()
    {
        SwapYSelectedNotes();
    }

    void OnClickShiftToLeftBeatButton()
    {
        ShiftSelectedNotes(HDirection.Left);
    }

    void OnClickShiftToRightBeatButton()
    {
        ShiftSelectedNotes(HDirection.Right);
    }

    void OnClickFeatToLeftBeatButton()
    {
        FeatSelectedNotesToBeat(HDirection.Left);
    }

    void OnClickFeatToRightBeatButton()
    {
        FeatSelectedNotesToBeat(HDirection.Right);
    }

    void OnClickFeatToClosetBeatButton()
    {
        FeatSelectedNotesToClosetBeat();
    }

    void OnClickCopyButton()
    {
        CopySelectedNotes();
    }

    void OnClickPasteToClosetBeatButton()
    {
        List<float> beats = mediator.music.GetBeats(mediator.noteViewport.beatType);
        int closetIndex = mediator.music.GetClosetBeatIndexAtTime(mediator.music.playingTime, mediator.noteViewport.beatType);
        PasteToTime(beats[closetIndex]);
    }

    void DestroySelectedNotes()
    {
        selectedNotes.ForEach((x) => mediator.music.DestroyNote(x));
        Clear();
    }

    void SwapYSelectedNotes()
    {
        selectedNotes.ForEach((x) => {
            x.SwapY();
            x.SetSelect(true);
            });
    }

    void ShiftSelectedNotes(HDirection direction)
    {
        selectedNotes.ForEach((x) =>
        {
            x.Shift(mediator.noteViewport.beatType, direction);
        });
    }

    void FeatSelectedNotesToBeat(HDirection direction)
    {
        selectedNotes.ForEach((x) =>
        {
            x.FitToBeat(mediator.noteViewport.beatType, direction);
        });
    }

    void FeatSelectedNotesToClosetBeat()
    {
        selectedNotes.ForEach((x) =>
        {
            x.FitToClosetBeat(mediator.noteViewport.beatType);
        });
    }

    void CopySelectedNotes()
    {
        // ����Ǿ� �ӽ� ����� ��Ʈ���� �ʱ�ȭ ������ ��ġ�� ���� ����Դϴ�.
        // �̿� ���� OnDestroyInstance �Լ��� ȣ����� �ʽ��ϴ�.
        copiedTempNotes.ForEach((x) => mediator.music.DestroyNote(x));
        copiedTempNotes.Clear();

        selectedNotes.ForEach((x) =>
        {
            Note copied = Instantiate<Note>(x);
            copied.Init_CopyInEditor(x);
            copied.transform.SetParent(copiedTempNoteStorageObject.transform);
            copiedTempNotes.Add(copied);
        });

        // �ð� �������� �������� �����մϴ�.
        copiedTempNotes.Sort((lhs, rhs) => lhs.data.time.CompareTo(rhs.data.time));

        if(copiedTempNotes.Count > 0)
        {
            // ������ ��Ʈ���� ��� ���� �ð��� ���� ��带 �������� ������ ������ �̵��ϰ� �˴ϴ�.

            // ex) 10, 15, 20, 30, 40, 60 �� ��Ʈ�� �ֽ��ϴ�.
            // ���� ���� �ð��� ��Ʈ�� �ð��� 10��ŭ ��� ��Ʈ�� �ð����� ���ҽ�ŵ�ϴ�.
            // ������ �ӽ� ��Ʈ���� �ð��� 0, 5, 10, 20, 30, 50 �� �˴ϴ�.

            // �� �� �ٿ��ֱ⸦ �� �� �ٿ��ֱ⸦ �� �ð��� ��������
            // �ٿ��ֱ⸦ �� �ð����� ���ʴ�� ��ġ�˴ϴ�.

            float minTime = copiedTempNotes[0].data.time;
            copiedTempNotes.ForEach((x) => x.data.time -= minTime);
        }
    }

    void PasteToTime(float time)
    {
        // ���� ������ ����մϴ�.
        Clear();

        copiedTempNotes.ForEach((x) =>
        {
            Note newNote = Instantiate<Note>(x);
            newNote.gameObject.SetActive(true);
            newNote.data.time += time;
            newNote.Init_PasteInEditor(x);
            newNote.Init_CreateInEditor(false);
            mediator.music.AddNote(newNote);

            newNote.UpdateNote();

            // �ٿ��ֱ� �� ��Ʈ���� �����մϴ�.
            ToSelect(newNote);
        });
    }

    void ToSelect(Note note)
    {
        int index = selectedNotes.FindIndex((x) => x.data.hash == note.data.hash);
        if(index == -1)
        {
            selectedNotes.Add(note);
            note.SetSelect(true); 
        }

        if(selectedNotes.Count == 1)
        {
            mediator.noteInsepctor.SetNote(note);
        }
        else
        {
            mediator.noteInsepctor.Clear();
        }
    }

    void ToggleSelect(Note note)
    {
        int index = selectedNotes.FindIndex((x) => x.data.hash == note.data.hash);
        if (index >= 0)
        {
            selectedNotes.RemoveAt(index);
            note.SetSelect(false);
        }
        else
        {
            selectedNotes.Add(note);
            note.SetSelect(true);
        }

        mediator.noteInsepctor.Clear();
    }

    void Clear()
    {
        ClearSelect();
        ClearDrag();
    }

    void ClearSelect()
    {
        selectedNotes.ForEach((x) =>
        {
            if (x && x.gameObject)
            {
                x.SetSelect(false);
            }
        });
        selectedNotes.Clear();

        mediator.noteInsepctor.Clear();
    }

    void ClearDrag()
    {
        dragSelecteNotes = false;
        dragNotes.Clear();
    }

    void OnSelectBeat(float time, NotePosition position)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }
    }

    void OnSelectNotes(List<Note> notes, NoteElementHandle elementHandle, SelectMouseType eventType)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (eventType == SelectMouseType.MouseDown)
        {
            dragSelecteNotes = false;
            if (selectMode == SelectMode.ReplaceOrDrag && notes.Count == 1)
            {
                if (selectedNotes.Contains(notes[0]))
                {
                    // ��Ʈ �巡�� ����

                    dragNotes.Clear();

                    if (elementHandle)
                    {
                        DragNote dragNoteElementHandle = new DragNote()
                        {
                            originTime = elementHandle.GetTime(),
                            note = elementHandle,
                        };
                        dragNotes.Add(dragNoteElementHandle);
                    }
                    else
                    {
                        selectedNotes.ForEach((x) =>
                        {
                            DragNote dragNote = new DragNote()
                            {
                                originTime = x.data.time,
                                note = x,
                            };
                            dragNotes.Add(dragNote);
                        });
                    }

                    Vector2 worldMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector2 hitPointSpaceLocalMouse = mediator.hitPoint.transform.InverseTransformPoint(worldMouse);
                    float mouseTime = mediator.music.LocalXToTime(hitPointSpaceLocalMouse.x, mediator.music.playingTime);

                    int closetBeatIndex;
                    NotePosition closetBeatPosition;
                    if (mediator.noteViewport.ClosetBeatOnWorld(worldMouse, out closetBeatIndex, out closetBeatPosition))
                    {
                        List<float> beats = mediator.music.GetBeats(mediator.noteViewport.beatType);
                        dragStartClosetBeatTime = beats[closetBeatIndex];

                        dragSelecteNotes = true;
                        return;
                    }
                }
            }
        }

        switch (selectMode)
        {
            case SelectMode.ReplaceOrDrag:
            ClearSelect();
            break;

            case SelectMode.Toggle:
            break;

            case SelectMode.Append:
            break;
        }

        foreach (Note note in notes)
        {
            switch (selectMode)
            {
                case SelectMode.ReplaceOrDrag:
                ToSelect(note);
                break;

                case SelectMode.Toggle:
                ToggleSelect(note);
                break;

                case SelectMode.Append:
                ToSelect(note);
                break;
            }
        }
    }

    void OnSelectEmpty(SelectMouseType eventType)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (eventType == SelectMouseType.MouseUp && dragSelecteNotes)
        {
            return;
        }

        Clear();
    }

    private void Awake()
    {
        copiedTempNoteStorageObject = new GameObject($"{gameObject.name}(Copied Note Storage)");
        copiedTempNoteStorageObject.SetActive(false);

        destroyButton.onClick.AddListener(OnClickDestroyButton);
        swapYButton.onClick.AddListener(OnClickSwapYButton);
        shiftToLeftBeatButton.onClick.AddListener(OnClickShiftToLeftBeatButton);
        shiftToRightBeatButton.onClick.AddListener(OnClickShiftToRightBeatButton);
        fitToLeftBeatButton.onClick.AddListener(OnClickFeatToLeftBeatButton);
        fitToRightBeatButton.onClick.AddListener(OnClickFeatToRightBeatButton);
        fitToClosetBeatButton.onClick.AddListener(OnClickFeatToClosetBeatButton);
        copyButton.onClick.AddListener(OnClickCopyButton);
        pasteToClosetBeatButton.onClick.AddListener(OnClickPasteToClosetBeatButton);
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {
            dragSelecteNotes = false;
        }
        if (dragSelecteNotes)
        {
            Vector2 worldMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 hitPointSpaceLocalMouse = mediator.hitPoint.transform.InverseTransformPoint(worldMouse);
            float mouseTime = mediator.music.LocalXToTime(hitPointSpaceLocalMouse.x, mediator.music.playingTime);

            int closetBeatIndex;
            NotePosition closetBeatPosition;
            if (mediator.noteViewport.ClosetBeatOnWorld(worldMouse, out closetBeatIndex, out closetBeatPosition))
            {
                List<float> beats = mediator.music.GetBeats(mediator.noteViewport.beatType);
                float closetBeatTime = beats[closetBeatIndex];
                float timeDiff = closetBeatTime - dragStartClosetBeatTime;

                foreach(DragNote dragNote in dragNotes)
                {
                    dragNote.note.SetTime(dragNote.originTime + timeDiff);
                }
            }
        }

        if(Input.GetKey(Keys.LShift) || Input.GetKey(Keys.RShift))
        {
            selectMode = SelectMode.Append;
        }
        else if(Input.GetKey(Keys.LCnt) || Input.GetKey(Keys.RCnt))
        {
            selectMode = SelectMode.Toggle;
        }
        else
        {
            selectMode = SelectMode.ReplaceOrDrag;
        }

        if (!Utility.UsingInputField())
        {
            if (Input.GetKeyDown(Keys.DestroyNote))
            {
                DestroySelectedNotes();
            }
            else if (Input.GetKeyDown(Keys.SwapNoteY))
            {
                SwapYSelectedNotes();
            }
            else if (Input.GetKeyDown(Keys.ShiftNoteToLeft))
            {
                ShiftSelectedNotes(HDirection.Left);
            }
            else if (Input.GetKeyDown(Keys.ShiftRightToLeft))
            {
                ShiftSelectedNotes(HDirection.Right);
            }
            else if (Input.GetKeyDown(Keys.FitNoteToBeat))
            {
                OnClickFeatToClosetBeatButton();
            }
            else if ((Input.GetKey(Keys.LCnt) || Input.GetKey(Keys.RCnt)) && Input.GetKeyDown(Keys.CopyNote))
            {
                CopySelectedNotes();
            }
            else if ((Input.GetKey(Keys.LCnt) || Input.GetKey(Keys.RCnt)) && Input.GetKeyDown(Keys.PasteNote))
            {
                int closetBeatIndex;
                NotePosition closetNotePosition;
                if (mediator.noteViewport.ClosetBeatOnScreen(Input.mousePosition, out closetBeatIndex, out closetNotePosition))
                {
                    List<float> beats = mediator.music.GetBeats(mediator.noteViewport.beatType);
                    PasteToTime(beats[closetBeatIndex]);
                }
            }
        }
    }

    private void OnEnable()
    {
        mediator.noteViewport.OnSelectBeat += OnSelectBeat;
        mediator.noteViewport.OnSelectNotes += OnSelectNotes;
        mediator.noteViewport.OnSelectEmpty += OnSelectEmpty;

        mediator.noteViewport.enableDrag = true;

        Clear();
    }

    private void OnDisable()
    {
        if (mediator)
        {
            mediator.noteViewport.OnSelectBeat -= OnSelectBeat;
            mediator.noteViewport.OnSelectNotes -= OnSelectNotes;
            mediator.noteViewport.OnSelectEmpty -= OnSelectEmpty;

            mediator.noteViewport.enableDrag = false;

            Clear();
        }
    }
}
