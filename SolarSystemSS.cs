using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolarSystemSS : MonoBehaviour {

    Dictionary<string, GameObject> bodies; //holds all the celestial bodies in the scene
    float G = 1000.0f; //universal gravitational constant

    bool gamePaused;  //stores true if game paused, false if not

    //called once when the game starts
    void Start() {
        bodies = new Dictionary<string, GameObject>(); //initialising bodies on game start
        gameObject.name = "SolarSystemSS"; //changing the empty object name to SolarSystemSS
        gamePaused = false; //game is not paused when it starts
        
        //destroys children of the empty object
        //so they don't interfere with the working
        DestoryExistingBodies();

        //picks the data from SolarSytemData (decalred at the bottom)
        //and creates bodies in the scene. Also adds Rigidbody, and Trailrenderer
        CreateBodies(); 

        //changes camera position to fit the scene. Puts a solid black color background
        //changes the farthest viewable plane distance to 10000 units
        HandleCameraView();

        //applies the sideways force on the bodies necessary to keep them in orbit
        ApplyForce(InitialForce);
    }
    
        private void DestoryExistingBodies() {
            //looping over all children of empty game object
            for (int i = 0; i < transform.childCount; i++) {
                //starts a coroutine that waits till the end of the frame to destroy the object
                //without this Destroy or DestroyImmediate function won't work
                StartCoroutine(CustomDestroy(transform.GetChild(i).gameObject));
            }
        }

            //Custom destroying function, returns an IEnumerator, ends the coroutine
            //and then destroys the object by DestroyImmediate
            private IEnumerator CustomDestroy(GameObject go) {
                yield return new WaitForEndOfFrame(); //waiting till End of the frame
                DestroyImmediate(go); //destroying the object
            }

        private void CreateBodies() {
            //looping over every celestial body given in solarSystemData
            foreach (var body in solarSystemData) {
                //creating a sphere for every body
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                
                //gives the body a position, scale and
                //makes it the child of another body as given in solarSystemData
                go = AddTransformComponent(go, body); 

                //adds Rigidbody component to the body and gives it mass
                //sets angular drag to zero
                go = AddRigidBodyComponent(go, body);

                //gives the body a trail and assigns length of the trail
                //on the basis of the body's orbit length
                go = AddTrailRendererComponent(go, body);

                //adding the GameObject to the bodies dictionary
                this.bodies.Add(body.Key, go);
            }

        }

            private GameObject AddTransformComponent(
                GameObject go,
                KeyValuePair<string, Dictionary<string, System.Object>> kvp
            ) {
                go.name = kvp.Key; //setting name
                go.transform.position = (Vector3) kvp.Value["position"]; //setting position

                //checking by ternary operator if the body has no firstParent (means it is the center or sun)
                //if true then it is made a child of the empty gameobject else it is made child of the object 
                //given in solarSystemData
                go.transform.parent = (string) kvp.Value["firstParent"] == "" ?  
                    transform : bodies[(string) kvp.Value["firstParent"]].transform; 
                
                go.transform.localScale = new Vector3( //assigns scale to the body
                    (float) kvp.Value["diameter"],
                    (float) kvp.Value["diameter"],
                    (float) kvp.Value["diameter"]
                );
                
                return go;
            }

            private GameObject AddRigidBodyComponent(
                GameObject go,
                KeyValuePair<string, Dictionary<string, System.Object>> kvp
            ) {
                Rigidbody rb = go.AddComponent<Rigidbody>();
                rb.mass = (float) kvp.Value["mass"]; //setting mass
                rb.angularDrag = 0.0f; //setting angular drag to zero as bodies are in space
                rb.useGravity = false; //if it is true then bodies will fall down
                
                return go;
            }

            private GameObject AddTrailRendererComponent(
                GameObject go,
                KeyValuePair<string, Dictionary<string, System.Object>> kvp
            ) {
                TrailRenderer tr = go.AddComponent<TrailRenderer>();
                tr.sharedMaterial = new Material(Shader.Find("Sprites/Default")); //sets the material

                //if the body has one parent (like the nine planets have only sun as the parent)
                //then they are given blue trail color else they are given red trail color
                tr.sharedMaterial.color = go.transform.parent.parent == transform ?
                    Color.blue : Color.red;
                
                //gives trail different thickness along its length
                AnimationCurve curve = new AnimationCurve();
                curve.AddKey(0.0f, 1.0f); //max thickness at the head
                curve.AddKey(1.0f, 0.0f); //min thickness at the tail
                tr.widthCurve = curve;

                //sets the trail length (it is measured in time) according
                //to the orbit length
                tr.time = (go.transform.parent.position - 
                    go.transform.position).magnitude / 5.0f;

                return go;
            }

        private void HandleCameraView() {
            Camera.main.transform.position = new Vector3(650, 70, 0); //sets camera position
            Camera.main.transform.rotation = Quaternion.Euler(0, 270, 0); //sets camera's tilt or rotation

            Camera.main.clearFlags = CameraClearFlags.SolidColor; //puts a solid color background
            Camera.main.backgroundColor = Color.black; //puts black color as background

            //sets longest viewable distance to 10000.0f
            //by default it is 1000.0f and makes some planets vanish
            //when they are too far
            Camera.main.farClipPlane = 10000.0f; 
        }
        
    //called once per every frame, affected by timescale
    void FixedUpdate() {
        //applies constant force of gravity
        //one child experiences force from
        //all its parents
        ApplyForce(GravitationalForce);

        HandleCameraMovement(); //updates camera or player movement

        //scales body trail width or thickness according 
        //to the distance between the camera and the body
        //if camera is far away then trail would thicken
        //keeping itself visible
        ScaleTrailWidth();
    }
    
        //this function is used to apply any force on the bodies
        //takes another function as parameter which describes 
        //the nature of force (initial force or gravitational force)
        private void ApplyForce(Force force) {
            //looping over all bodies
            foreach (string childName in bodies.Keys) {
                //looping over all their parents
                foreach (string parentName in (string[]) solarSystemData[childName]["allParents"]) {
                    if (parentName != "") { //if the body is not sun (sun experiences no force)
                        //fetches the Rigidbody component of the child and applies force
                        //according to the received force function
                        bodies[childName].GetComponent<Rigidbody>().AddForce(force( //force function takes 3 params
                            bodies[parentName].GetComponent<Rigidbody>().mass, //parent's mass
                            bodies[childName].GetComponent<Rigidbody>().mass, //child's mass
                            bodies[parentName].transform.position -
                                bodies[childName].transform.position //displacement vector between parent and child
                        ));
                    }
                }
            }
        }

            //delegate that allows to pass function as parameter (damn c#)
            private delegate Vector3 Force(float M, float m, Vector3 r);

            //calculates and returns initial sideways force necessary to keep body in orbit
            private Vector3 InitialForce(float M, float m, Vector3 r) {
                //this formula uses Time.deltaTime
                //basically it applies enough force in just frame to give the body
                //enough velocity to keep itself in the orvit and avoid getting dragged into its parent
                return new Vector3(0, 0, (m / Time.deltaTime) * (float)Mathf.Pow(G * M / r.magnitude, 0.5f));
            }

            //calculates and returns the constant gravitational force
            private Vector3 GravitationalForce(float M, float m, Vector3 r) {
                //old newton's formula
                return r * (float)((G * M * m) / Mathf.Pow(r.magnitude, 3.0f));
            }

        private void HandleCameraMovement() {
            Transform camTf = Camera.main.transform; //storing camera transform in a temporary variable
            
            //by using mouse as input, altering the rotation of the camera i.e. eye movement
            Camera.main.transform.Rotate(new Vector3(
                -Input.GetAxis("Mouse Y"),
                Input.GetAxis("Mouse X"),
                0
            ) * Time.deltaTime * 100.0f); //100.0f is senstivity 

            if (Input.GetKey(KeyCode.W)) {
                camTf.position += camTf.forward; //forward if pressed w
            } else if (Input.GetKey(KeyCode.S)) {
                camTf.position -= camTf.forward; //backward if pressed s
            } else if (Input.GetKey(KeyCode.A)) {
                camTf.position -= camTf.right; //left if pressed a
            } else if (Input.GetKey(KeyCode.D)) {
                camTf.position += camTf.right; //right if pressed d
            }
        }
            
        private void ScaleTrailWidth() {
            foreach (GameObject go in bodies.Values) {
                go.GetComponent<TrailRenderer>().widthMultiplier = 
                (Camera.main.transform.position - go.transform.position)
                .magnitude / 150.0f; //scaling by a factor of 150.0f, found experimentally
            }
        }

     //runs once per every frame and unaffected by timescale
    void Update() {
        ToggleGamePause(); //pauses or unpauses the game
    }

        private void ToggleGamePause() {
            if (Input.GetKeyDown(KeyCode.Escape)) { //if escape key is pressed
                gamePaused = !gamePaused;   //change the game pause or unpause state
            }

            //if game is paused then make the mouse pointer visible again
            //and stop the time in the game
            if (gamePaused) {
                Cursor.lockState = CursorLockMode.None; 
                Time.timeScale = 0;
            } 
            //else lock the mouse pointer and get normal timescale
            else {
                Cursor.lockState = CursorLockMode.Locked;
                Time.timeScale = 1;
            }
        }
            
    //solarSystemData, holds all the information required for the generation
    //of the solar system through above code
    Dictionary<string, Dictionary<string, System.Object>> solarSystemData =
        new Dictionary<string, Dictionary<string, System.Object>>() {
            { "sun", new Dictionary<string, System.Object>() {
                { "position", Vector3.zero },
                { "diameter", 100.0f },
                { "mass", 100.0f },
                { "firstParent", "" },
                { "allParents", new string[] { "" } }
            } },
            { "mercury", new Dictionary<string, System.Object>() {
                { "position", new Vector3(62.5f, 0, 0) },
                { "diameter", 0.01f },
                { "mass", 10.0f },
                { "firstParent", "sun" },
                { "allParents", new string[] { "sun" } }
            } },
            { "venus", new Dictionary<string, System.Object>() {
                { "position", new Vector3(93.75f, 0, 0) },
                { "diameter", 0.02f },
                { "mass", 22.5f },
                { "firstParent", "sun" },
                { "allParents", new string[] { "sun" } }
            } },
            { "earth", new Dictionary<string, System.Object>() {
                { "position", new Vector3(125.0f, 0, 0) },
                { "diameter", 0.05f },
                { "mass", 25.0f },
                { "firstParent", "sun" },
                { "allParents", new string[] { "sun" } }
            } },
            { "moon", new Dictionary<string, System.Object>() {
                { "position", new Vector3(130.0f, 0, 0) },
                { "diameter", 0.1f },
                { "mass", 1.0f },
                { "firstParent", "earth" },
                { "allParents", new string[] { "sun", "earth" } }
            } },
            { "mars", new Dictionary<string, System.Object>() {
                { "position", new Vector3(156.25f, 0, 0) },
                { "diameter", 0.025f },
                { "mass", 17.5f },
                { "firstParent", "sun" },
                { "allParents", new string[] { "sun" } }
            } },
            { "jupiter", new Dictionary<string, System.Object>() {
                { "position", new Vector3(250.0f, 0, 0) },
                { "diameter", 0.25f },
                { "mass", 75.0f },
                { "firstParent", "sun" },
                { "allParents", new string[] { "sun" } }
            } },
            { "io", new Dictionary<string, System.Object>() {
                { "position", new Vector3(265.0f, 0, 0) },
                { "diameter", 0.05f },
                { "mass", 1.0f },
                { "firstParent", "jupiter" },
                { "allParents", new string[] { "sun", "jupiter" } }
            } },
            { "europa", new Dictionary<string, System.Object>() {
                { "position", new Vector3(267.5f, 0, 0) },
                { "diameter", 0.05f },
                { "mass", 1.0f },
                { "firstParent", "jupiter" },
                { "allParents", new string[] { "sun", "jupiter" } }
            } },
            { "ganymede", new Dictionary<string, System.Object>() {
                { "position", new Vector3(270.0f, 0, 0) },
                { "diameter", 0.05f },
                { "mass", 1.0f },
                { "firstParent", "jupiter" },
                { "allParents", new string[] { "sun", "jupiter" } }
            } },
            { "callisto", new Dictionary<string, System.Object>() {
                { "position", new Vector3(272.5f, 0, 0) },
                { "diameter", 0.05f },
                { "mass", 1.0f },
                { "firstParent", "jupiter" },
                { "allParents", new string[] { "sun", "jupiter" } }
            } },
            { "saturn", new Dictionary<string, System.Object>() {
                { "position", new Vector3(312.5f, 0, 0) },
                { "diameter", 0.2f },
                { "mass", 70.0f },
                { "firstParent", "sun" },
                { "allParents", new string[] { "sun" } }
            } },
            { "uranus", new Dictionary<string, System.Object>() {
                { "position", new Vector3(375.0f, 0, 0) },
                { "diameter", 0.175f },
                { "mass", 40.0f },
                { "firstParent", "sun" },
                { "allParents", new string[] { "sun" } }
            } },
            { "miranda", new Dictionary<string, System.Object>() {
                { "position", new Vector3(385.0f, 0, 0) },
                { "diameter", 0.05f },
                { "mass", 1.0f },
                { "firstParent", "uranus" },
                { "allParents", new string[] { "sun", "uranus" } }
            } },
            { "ariel", new Dictionary<string, System.Object>() {
                { "position", new Vector3(387.5f, 0, 0) },
                { "diameter", 0.05f },
                { "mass", 1.0f },
                { "firstParent", "uranus" },
                { "allParents", new string[] { "sun", "uranus" } }
            } },
            { "umbriel", new Dictionary<string, System.Object>() {
                { "position", new Vector3(390.0f, 0, 0) },
                { "diameter", 0.05f },
                { "mass", 1.0f },
                { "firstParent", "uranus" },
                { "allParents", new string[] { "sun", "uranus" } }
            } },
            { "titania", new Dictionary<string, System.Object>() {
                { "position", new Vector3(392.5f, 0, 0) },
                { "diameter", 0.05f },
                { "mass", 1.0f },
                { "firstParent", "uranus" },
                { "allParents", new string[] { "sun", "uranus" } }
            } },
            { "oberon", new Dictionary<string, System.Object>() {
                { "position", new Vector3(395.0f, 0, 0) },
                { "diameter", 0.05f },
                { "mass", 1.0f },
                { "firstParent", "uranus" },
                { "allParents", new string[] { "sun", "uranus" } }
            } },
            { "neptune", new Dictionary<string, System.Object>() {
                { "position", new Vector3(437.5f, 0, 0) },
                { "diameter", 0.175f },
                { "mass", 35.0f },
                { "firstParent", "sun" },
                { "allParents", new string[] { "sun" } }
            } },
            { "pluto", new Dictionary<string, System.Object>() {
                { "position", new Vector3(500.0f, 0, 0) },
                { "diameter", 0.01f },
                { "mass", 10.0f },
                { "firstParent", "sun" },
                { "allParents", new string[] { "sun" } }
            } }, 
        };
}