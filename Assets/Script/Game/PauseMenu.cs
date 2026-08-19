using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
        public GameObject pauseMenu;
        public bool IsPauseMenuOpen{ get { return pauseMenu.activeSelf; } }

        public Sprite defenseControlSprite;
        public Sprite attackControlSprite;
        public Image controlImage;
        private bool _showDefense;
        
        public void Start()
        {
                pauseMenu.SetActive(false);
                controlImage.sprite = defenseControlSprite;
                _showDefense = true;
        }

        public void Update()
        {
                if ((Input.GetKeyDown(RemoteInput.START1) || Input.GetKeyDown(RemoteInput.START2)) && GameManager.Instance.IsMatchEnd == false)
                {
                        if (IsPauseMenuOpen)
                        {
                                CloseMenu();
                        }
                        else
                        {
                                OpenMenu();
                        }
                }
        }

        public void OpenMenu()
        {
                GameManager.Instance.matchPlaying = false;
                pauseMenu.SetActive(true);
        }

        public void CloseMenu()
        {
                GameManager.Instance.matchPlaying = true;
                pauseMenu.SetActive(false);
        }

        public void ReturnToCharacterSelection()
        {
                SceneTransitor.Instance.LoadScene(1);
        }

        public void SwapControl()
        {
                Debug.Log("SwapControl");
                if (_showDefense == true)
                {
                        controlImage.sprite = attackControlSprite;
                        _showDefense = false;
                }
                else
                {
                        controlImage.sprite = defenseControlSprite;
                        _showDefense = true;
                }
        }
}
