using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

public sealed class Ref<T>
{
    private Func<T> getter;
    private Action<T> setter;
    public Ref(Func<T> getter, Action<T> setter)
    {
        this.getter = getter;
        this.setter = setter;
    }
    public T Value
    {
        get { return getter(); }
        set { setter(value); }
    }
}

public enum FieldAccessType
{
    // ������ ����
    Always,
    // ���� �������� ����
    Never,
    // ���ǿ� ���� ����
    Condition,
}

public class DepthFieldInfo
{
    DepthFieldInfo _parent;
    FieldInfo _fieldInfo;

    public FieldInfo fieldInfo => _fieldInfo;
    public DepthFieldInfo parent => _parent;

    public DepthFieldInfo(FieldInfo fieldInfo, DepthFieldInfo parent)
    {
        _fieldInfo = fieldInfo;
        _parent = parent;
    }

    public DepthFieldInfo(FieldInfo fieldInfo) : this(fieldInfo, null) { }

    public object GetValue<T>(T rootData)
    {
        Stack<DepthFieldInfo> s = new Stack<DepthFieldInfo>();
        // {��Ʈ ������ �ڽ�, �ڽ�a, �ڽ�b, ����}
        // �̿� ���� ������ ���ÿ� ���´�. (���� ������ Pop�ȴ�)
        DepthFieldInfo currentDepth = this;
        while (currentDepth != null)
        {
            s.Push(currentDepth);
            currentDepth = currentDepth.parent;
        }
        // ��Ʈ���� ���� �������� ���������� �İ���.
        object currentObject = rootData;
        while (s.Count > 0)
        {
            DepthFieldInfo depth = s.Pop();
            currentObject = depth.fieldInfo.GetValue(currentObject);
        }
        return currentObject;
    }

    public void SetValue<T>(Ref<T> refRootData, object value)
    {
        object originRootData = refRootData.Value;
        Stack<DepthFieldInfo> s = new Stack<DepthFieldInfo>();
        // {��Ʈ ������ �ڽ�, �ڽ�a, �ڽ�b, ����}
        // �̿� ���� ������ ���ÿ� ���´�. (���� ������ Pop�ȴ�)
        DepthFieldInfo currentDepth = this;
        while (currentDepth != null)
        {
            s.Push(currentDepth);
            currentDepth = currentDepth.parent;
        }
        Queue<Temp> q = new Queue<Temp>();
        // ���� ������Ʈ�� ��Ʈ�� ������ �ֻ�� �θ� ��Ʈ�� �ǵ��� �Ѵ�.
        object currentObject = originRootData;
        // 1���� ���ܵд�.
        // ��, ���� �����Ϸ��� ������ �������� �ʴ´�.
        // {(����<-�ڽ�b), (�ڽ�b<-�ڽ�a), (�ڽ�a<-��Ʈ ������ �ڽ�), (��Ʈ ������ �ڽ�<-��Ʈ)}
        // �̿� ���� ������ ť�� ���´�. (�ڽ�<-�θ� �����̸�, ���� ������ Pop�ȴ�)
        while (s.Count > 1)
        {
            DepthFieldInfo top = s.Pop();

            Temp temp = new Temp() { childFieldInfo = top.fieldInfo, parentObject = currentObject };
            currentObject = top.fieldInfo.GetValue(currentObject);
            temp.childObject = currentObject;

            q.Enqueue(temp);
        }
        // ť�� 1��(�����Ϸ��� ����)�� �����ֱ⿡ currentObject�� �����Ϸ��� ������ �θ��̴�.
        // �����Ϸ��� ������ �θ𿡼� �����Ϸ��� �������� ���� �����Ѵ�.
        this.fieldInfo.SetValue(currentObject, value);
        // ����� ������ �θ� ���������� ��Ʈ���� �����Ѵ�.
        while (q.Count > 0)
        {
            Temp front = q.Dequeue();
            front.childFieldInfo.SetValue(front.parentObject, front.childObject);
        }
        refRootData.Value = (T)originRootData;
    }

    struct Temp
    {
        public object parentObject;
        public FieldInfo childFieldInfo;
        public object childObject;
    }

    public void SetValue<T>(ref T rootData, object value)
    {
        object originRootData = rootData;

        Stack<DepthFieldInfo> s = new Stack<DepthFieldInfo>();
        DepthFieldInfo currentDepth = this;
        while (currentDepth != null)
        {
            s.Push(currentDepth);
            currentDepth = currentDepth.parent;
        }

        Queue<Temp> q = new Queue<Temp>();
        object currentObject = originRootData;
        while (s.Count > 1)
        {
            DepthFieldInfo top = s.Pop();

            Temp temp = new Temp() { childFieldInfo = top.fieldInfo, parentObject = currentObject };
            currentObject = top.fieldInfo.GetValue(currentObject);
            temp.childObject = currentObject;

            q.Enqueue(temp);
        }

        this.fieldInfo.SetValue(currentObject, value);

        while(q.Count > 0)
        {
            Temp front = q.Dequeue();
            front.childFieldInfo.SetValue(front.parentObject, front.childObject);
        }

        rootData = (T)originRootData;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class FieldAccessAttribute : PropertyAttribute
{
    Type _type;
    string _name;
    object _value;
    FieldAccessType _access;

    public Type type => _type;
    public string name => _name;
    public object value => _value;
    public FieldAccessType access => _access;

    /// <summary>
    /// �ش� �ʵ尡 ������ ���� �����ϰų�, ������ ���� �Ұ����ϵ��� �����մϴ�.
    /// </summary>
    /// <param name="access"></param>
    public FieldAccessAttribute(bool access)
    {
        if (access)
        {
            _access = FieldAccessType.Always;
        }
        else
        {
            _access = FieldAccessType.Never;
        }
    }

    /// <summary>
    /// �ش� �ʵ尡 ������ Ÿ���� �̸��� ���� ��� �ʵ��� ���� ���� ���� ���� �����ϵ��� �����մϴ�.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public FieldAccessAttribute(Type type, string name, object value)
    {
        _type = type;
        _name = name;
        _value = value;
        _access = FieldAccessType.Condition;
    }

    /// <summary>
    /// �ش� �ʵ尡 ���� ������ �ʵ����� Ȯ���մϴ�.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data">�ش� �ʵ尡 ���ԵǾ� �ִ� ��ü�Դϴ�.</param>
    /// <param name="fieldInfo">�ش� �ʵ��� �����Դϴ�.</param>
    /// <returns></returns>
    public static bool IsAccessibleField<T>(T data, FieldInfo fieldInfo)
    {
        List<FieldAccessAttribute> attributes = fieldInfo.GetCustomAttributes<FieldAccessAttribute>().ToList();

        // �ش� �ʵ忡 �Ӽ��� �����Ǿ� ���� �ʴٸ� ������ �� �����ϴ�.
        if (attributes.Count == 0)
        {
            return false;
        }

        // �������� �Ӽ��� ������ ��쿡�� OR ����ó�� �ϳ��� �����ϸ� ���� �����մϴ�.
        FieldInfo[] fieldInfos = data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldAccessAttribute attribute in attributes)
        {
            switch (attribute.access)
            {
                // ���� ���� ������ ��쿡�� ������ ������ �� �ֽ��ϴ�.
                case FieldAccessType.Always:
                return true;
                // ���� �Ұ� ������ ��쿡�� �ٸ� �Ӽ��� Ž���մϴ�.
                case FieldAccessType.Never:
                break;
                case FieldAccessType.Condition:
                {
                    FieldInfo[] matches = Array.FindAll(fieldInfos, (x) =>
                    {
                        return
                        x.FieldType == attribute.type &&
                        x.Name == attribute.name;
                    });
                    // Ÿ�԰� �̸��� ���� �ʵ尡 ���� ��쿡�� �ٸ� �Ӽ��� Ž���մϴ�.
                    if (matches.Count() == 0)
                    {
                        break;
                    }
                    // Ÿ�԰� �̸��� ���� �ʵ��� ���� ���Ե� ���� ���� ���� �ϳ��� �ִٸ� ������ �� �ֽ��ϴ�.
                    foreach (FieldInfo match in matches)
                    {
                        object matchValue = match.GetValue(data);
                        if (matchValue.Equals(attribute.value))
                        {
                            return true;
                        }
                    }
                }
                // Ÿ�� ��Ī�� �����ϸ� �ٸ� ������ Ž���մϴ�.
                break;
            }
        }
        // ������ ��� �����ϸ� �ش� �ʵ�� ���� �Ұ����մϴ�.
        return false;
    }

    /// <summary>
    /// ��ü���� ���� ������ �ʵ� �������� �����մϴ�.<para>
    /// �� �޼ҵ�� ��ü���� �ٸ� ��ü ������ ������ �������� �ʽ��ϴ�.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data">�ʵ� �������� ������ ��ü�Դϴ�.</param>
    /// <returns></returns>
    public static List<FieldInfo> GetAccessibleFieldInfos<T>(T data)
    {
        Type type = data.GetType();
        FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        List<FieldInfo> accessibles = new List<FieldInfo>();
        foreach (FieldInfo fieldInfo in fieldInfos)
        {
            if (FieldAccessAttribute.IsAccessibleField(data, fieldInfo))
            {
                accessibles.Add(fieldInfo);
            }
        }

        return accessibles;
    }

    /// <summary>
    /// ��ü���� ���� ������ �ʵ� �������� �����մϴ�.<para>
    /// �� �޼ҵ�� ��ü���� �ٸ� ��ü ������ ���� ���� �����մϴ�.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data">�ʵ� �������� ������ ��ü�Դϴ�.</param>
    /// <returns></returns>
    public static List<DepthFieldInfo> GetAccessibleFieldDepthInfos<T>(T data)
    {
        List<DepthFieldInfo> depthFieldInfos = new List<DepthFieldInfo>();
        Queue<DepthFieldInfo> q = new Queue<DepthFieldInfo>();

        void Search(List<FieldInfo> fieldInfos, DepthFieldInfo parent)
        {
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                DepthFieldInfo depth = new DepthFieldInfo(fieldInfo, parent);
                depthFieldInfos.Add(depth);

                bool isStruct = fieldInfo.FieldType.IsValueType && !fieldInfo.FieldType.IsPrimitive;
                bool isClass = fieldInfo.FieldType.IsClass;
                if (isStruct || isClass)
                {
                    q.Enqueue(depth);
                }
            }
        }
        Search(GetAccessibleFieldInfos(data), null);
        while (q.Count > 0)
        {
            DepthFieldInfo front = q.Dequeue();
            object value = front.GetValue(data);
            List<FieldInfo> infos = GetAccessibleFieldInfos(value);
            Search(infos, front);
        }
        return depthFieldInfos;
    }
}
