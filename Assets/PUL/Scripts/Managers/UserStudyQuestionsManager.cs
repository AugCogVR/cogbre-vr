using PUL;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

namespace PUL
{
    [System.Serializable]
    public class UserStudyQuestionsManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor

        public GameObject QuestionChooser;

        public bool userStudyQuestionsMoveable = true; // can users move user study questions?

        public TextMeshProUGUI questionTMP;
        public GameObject q1Button;
        public GameObject q2Button;
        public GameObject q3Button;
        public GameObject q4Button;
        public GameObject q5Button;
        public GameObject endButton;

        // END: These values are wired up in the Unity Editor
        // ====================================

        // Instance holder
        private static UserStudyQuestionsManager _instance; // this manager is a singleton

        public static UserStudyQuestionsManager Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("UserStudyQuestionsManager is NULL");
                return _instance;
            }
        }

        void Awake()
        {
            // If another instance exists, destroy that game object. If no other game manager exists, 
            // initialize the instance to itself. As this manager needs to exist throughout all scenes, 
            // call the function DontDestroyOnLoad.
            if (_instance)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            DontDestroyOnLoad(this);
        }

        // Start is called before the first frame update
        void Start()
        {
            // Enable/disable question chooser based on config
            bool userStudyQuestionsEnabled = false;
            string value = ConfigManager.Instance.GetFeatureSetProperty("user_study_questions_enabled");
            if (value != null) userStudyQuestionsEnabled = bool.Parse(value);
            QuestionChooser.SetActive(userStudyQuestionsEnabled);

            // Enable/disable movement based on config
            string value2 = ConfigManager.Instance.GetFeatureSetProperty("user_study_questions_moveable");
            if (value2 != null) userStudyQuestionsMoveable = bool.Parse(value2);
            // Overall slate object manipulator
            ObjectManipulator qcOM = QuestionChooser.GetComponent<ObjectManipulator>();
            qcOM.enabled = userStudyQuestionsMoveable;
            // Title bar object manipulator
            ObjectManipulator titleBarOM = QuestionChooser.transform.Find("TitleBar").gameObject.GetComponent<ObjectManipulator>();
            titleBarOM.enabled = userStudyQuestionsMoveable;

            // Wire up question button callbacks. Ugly wall of repetitive code!
            PressableButtonHoloLens2 q1ButtonFunction = q1Button.GetComponent<PressableButtonHoloLens2>();
            q1ButtonFunction.TouchBegin.AddListener(() => QuestionButtonCallback(1));
            Interactable q1DistanceInteract = q1Button.GetComponent<Interactable>();
            q1DistanceInteract.OnClick.AddListener(() => QuestionButtonCallback(1));

            PressableButtonHoloLens2 q2ButtonFunction = q2Button.GetComponent<PressableButtonHoloLens2>();
            q2ButtonFunction.TouchBegin.AddListener(() => QuestionButtonCallback(2));
            Interactable q2DistanceInteract = q2Button.GetComponent<Interactable>();
            q2DistanceInteract.OnClick.AddListener(() => QuestionButtonCallback(2));

            PressableButtonHoloLens2 q3ButtonFunction = q3Button.GetComponent<PressableButtonHoloLens2>();
            q3ButtonFunction.TouchBegin.AddListener(() => QuestionButtonCallback(3));
            Interactable q3DistanceInteract = q3Button.GetComponent<Interactable>();
            q3DistanceInteract.OnClick.AddListener(() => QuestionButtonCallback(3));

            PressableButtonHoloLens2 q4ButtonFunction = q4Button.GetComponent<PressableButtonHoloLens2>();
            q4ButtonFunction.TouchBegin.AddListener(() => QuestionButtonCallback(4));
            Interactable q4DistanceInteract = q4Button.GetComponent<Interactable>();
            q4DistanceInteract.OnClick.AddListener(() => QuestionButtonCallback(4));

            PressableButtonHoloLens2 q5ButtonFunction = q5Button.GetComponent<PressableButtonHoloLens2>();
            q5ButtonFunction.TouchBegin.AddListener(() => QuestionButtonCallback(5));
            Interactable q5DistanceInteract = q5Button.GetComponent<Interactable>();
            q5DistanceInteract.OnClick.AddListener(() => QuestionButtonCallback(5));

            PressableButtonHoloLens2 endButtonFunction = endButton.GetComponent<PressableButtonHoloLens2>();
            endButtonFunction.TouchBegin.AddListener(() => QuestionButtonCallback(6));
            Interactable endDistanceInteract = endButton.GetComponent<Interactable>();
            endDistanceInteract.OnClick.AddListener(() => QuestionButtonCallback(6));
        }

        // Update is called once per frame
        void Update()
        {
        }


        public void QuestionButtonCallback(int questionNum)
        {
            switch (questionNum)
            {
                case 1: 
                    questionTMP.text = "Question 1:\n\nIn this program, a user's input is considered correct when a function that asks for an input returns the user's input value instead of zero. What specific input does the function part1a require to return a nonzero value?";
                    break;

                case 2:
                    questionTMP.text = "Question 2:\n\nWhat final value is returned by part1d when the input to part1a is correct?";
                    break;

                case 3:
                    questionTMP.text = "Question 3:\n\nWhat final value is returned by part3d when the input to part3a is correct?";
                    break;

                case 4:
                    questionTMP.text = "Question 4:\n\nWhat final value is returned by part4e when the required inputs are correct?";
                    break;

                case 5:
                    questionTMP.text = "Question 5:\n\nHow many times is the function part1a called if the part1d function fully executes (you examined this chain of functions in question 2)?";
                    break;

                case 6:
                    questionTMP.text = "Thank you for your participation!";
                    break;
            }

            // Report selection event.
            string command = $"[\"session_update\", \"event\", \"question_select\", \"{questionNum}\"]";
            NexusClient.Instance.NexusSessionUpdate(command);
        }
     }
}
