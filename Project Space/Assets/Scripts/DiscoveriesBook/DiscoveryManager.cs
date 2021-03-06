﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Xml;
using System.IO;


public class DiscoveryManager : MonoBehaviour
{
    public DescriptionComponent discoveryDescriptionPrefab; // текст с описание выбранного открытия
    public ButtonComponent discoveryPrefab; // кнопка-элемнт списка открытий
    public ScrollRect discoveryScrollRect; // UI список открытий
    //
    private string fileName, lastName;
    private List<Discovery> discoveries; // временное решение (для реализации одноступенчатой иерархии)
    private List<Section> node; // !!! для многоуровневой иерархии (пока не используем)
    private Section section;
    private Discovery discovery;
    //
    private bool isFirstDiscovery = true;
    //
    [SerializeField]
    private GameObject discoveryBookUI;
    //
    private HUDManager HUDManager;
    //
    UIManager uiManager;

    private static DiscoveryManager Internal { get; set; }

    public static DiscoveryManager GetInstance()
    {
        if (Internal == null)
        {
            Internal = new DiscoveryManager();
        }

        return Internal;
    }

    void Awake()
    {
        Internal = this;

        HUDManager = FindObjectOfType<HUDManager>();
        uiManager = UIManager.GetInstance();
    }

    // ВЫДЕЛИТЬ Функции Open, Close, Build, Clean в отдельный вирутальный класс (интерфейс)
    // открыть окно книги
    public void Open(string _fileName)
    {
        if (fileName == string.Empty)
            return;
        fileName = _fileName;

        HUDManager.HideHud(true);
        discoveryBookUI.SetActive(true);
        uiManager.blockPlayerMovement(true);
        Load();
    }

    void Load()
    {
        // нужно загрузить только названия открытий
        // сейчас реализовано только 1-уровневая иерархия открытий
        // то-есть слева список открытий - справа описание
        // поэтому подгружаем весь список открытий и их описание в массивы

        if (lastName != string.Empty ? fileName != lastName : true) // проверка, чтобы не загружать уже загруженный файл
        {
            discoveries = new List<Discovery>();

            try // чтение элементов XML и загрузка значений атрибутов в массивы
            {
                TextAsset binary = Resources.Load<TextAsset>(fileName);
                XmlTextReader reader = new XmlTextReader(new StringReader(binary.text));

                int index = 0;
                while (reader.Read())
                {
                    if (reader.IsStartElement("section"))
                    {
                        section = new Section();
                        section.name = reader.GetAttribute("name");
                        section.discoveries = new List<Discovery>();
                        // !!! node.Add(section);

                        XmlReader inner = reader.ReadSubtree();
                        while (inner.ReadToFollowing("discovery"))
                        {
                            discovery = new Discovery();
                            discovery.name = inner.GetAttribute("name");
                            discovery.text = inner.GetAttribute("text");

                            // !!! node[index].discoveries.Add(discovery);
                            discoveries.Add(discovery); // временно 
                        }
                        inner.Close();

                        index++;
                    }
                }

                lastName = fileName;
                reader.Close();
            }
            catch (System.Exception error)
            {
                Debug.Log(this + " Ошибка чтения файла книги открытий: " + fileName + ".xml >> Error: " + error.Message);
                //scrollRect.gameObject.SetActive(false);
                lastName = string.Empty;
            }
        }

        BuildBook();
    }

    void BuildElement(Discovery discovery)
    {
        ButtonComponent cloneObj = Instantiate(discoveryPrefab) as ButtonComponent;
        DescriptionComponent desc = FindObjectOfType<DescriptionComponent>();

        cloneObj.text.text = discovery.name;
        cloneObj.rect.SetParent(discoveryScrollRect.content);

        if (isFirstDiscovery)
        {
            desc.text.text = discovery.text;
            isFirstDiscovery = false;
        }
    }

    void BuildBook()
    {
        foreach (Discovery discovery in discoveries)
        {
            BuildElement(discovery);
        }
    }

    void Clean()
    {
        discoveries.Clear();
        //node.Clear();
    }

    public void Close()
    {
        Clean();
        discoveryBookUI.SetActive(false);
        HUDManager.HideHud(false);
        StartCoroutine("StartWaiting", 0.25f);
    }

    // задержка нужна чтобы не строить сразу путь
    IEnumerator StartWaiting()
    {
        yield return new WaitForSeconds(0.25f);
        uiManager.blockPlayerMovement(false);
    }



    Discovery FindDiscoveryByName(string name)
    {
        foreach (Discovery discovery in discoveries)
        {
            if (discovery.name == name)
            {
                return discovery;
            }
        }

        return null;
    }

    public void OpenDiscovery(string name)
    {
        Discovery discovery = FindDiscoveryByName(name);
        DescriptionComponent desc = FindObjectOfType<DescriptionComponent>();
        desc.text.text = discovery.text;
    }
}

class Section
{
    public string name;
    public List<Discovery> discoveries;
}


class Discovery
{
    public string name;
    public string text;
}