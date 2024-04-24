using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Componente que permite animar una tela utilizando el m�todo masa-muelle.
public class MassSpringCloth : MonoBehaviour
{
    public bool paused; //Booleano que se encarga de pausar y reanudar la animaci�n.

    //Enumeraci�n de los m�todos de integraci�n a utilizar.
    public enum Integration
    {
        ExplicitEulerIntegration = 0,
        SymplecticEulerIntegration = 1,
    }

    public Integration integrationMethod; //Variable con la que escoger el m�todo de integraci�n a utilizar.

    Mesh mesh; //Mallado triangular de la tela.
    Vector3[] vertices; //Array que almacena en cada posici�n una copia de la posici�n 3D de cada v�rtice del mallado.
    List<Node> nodes; //Lista de objetos de la clase nodo que almacenan las propiedas f�sicas de los v�rtices del mallado para el c�lculo de la animaci�n.
    List<Spring> springs = new List<Spring>(); //Lista de objetos de la clase muelle que almacena las propiedades f�sicas de cada muelle y los 2 v�rtices que lo componen para el c�lculo de la animaci�n.
    int[] triangles; //Lista que almacena 3 enteros por tri�ngulo de la malla.
    List<Edge> edges = new List<Edge>(); //Lista que almacena todas las aristas de la malla.

    public float clothMass = 0.5f; //Masa total de la tela, repartida equitativamente entre cada uno de los nodos de masa que la componen.
    public float tractionSpringStiffness = 5.0f; //Constante de rigidez de los muelles de tracci�n. La tela no es muy el�stica.
    public float flexionSpringStiffness = 1.0f; //Constante de rigidez de los muelles de flexi�n. Sin embargo, s� se dobla f�cilmente.

    public float dAbsolute = 0.1f; //Constante de amortiguamiento (damping) absoluto sobre la velocidad de los nodos.
    public float dDeformation = 10f; //Constante de amortiguamiento de la deformaci�n de los muelles.

    public Vector3 g = new Vector3(0.0f, 9.8f, 0.0f); //Constante gravitacional.

    public float h = 0.01f; //Tama�o del paso de integraci�n de las f�sicas de la animaci�n.
    public int substeps = 1; //N�mero de subpasos. Se realiza la integraci�n las veces que indique por frame.
    private float h_def; //Paso efectivo finalmente utilizado en la integraci�n. Puede diferir de h en caso de que substeps > 1.

    // Start is called before the first frame update
    void Start()
    {
        paused = true; //Al comienzo de la ejecuci�n, la animaci�n se encuentra pausada.

        h_def = h / substeps; //El paso efectivo es igual al paso base divido entre el n�mero de subpasos a realizar por frame.

        mesh = gameObject.GetComponent<MeshFilter>().mesh; //Se almacena una referencia al mallado del gameObject.
        vertices = mesh.vertices; //Se almacena una copia de cada uno de los v�rtices del mallado en un array.
        nodes = new List<Node>(vertices.Length); //Se crea una lista con tantos nodos como v�rtices.

        for (int i = 0; i < vertices.Length; i++)
        {
            //Se insertan en la lista cada uno de los v�rtices, almacen�ndose su identificador, su posici�n en coordenadas globales, y la parte proporcional que le corresponde de la masa total de la tela.
            nodes.Add(new Node(i, transform.TransformPoint(vertices[i]), clothMass/vertices.Length));
        }

        //Se buscan los Fixer que hay en la escena.
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Fixer")) //Para cada Fixer
        {
            Fixer fixer = go.GetComponent<Fixer>();

            if (fixer != null)
            {
                foreach (Node node in nodes) //Para cada nodo
                {
                    if (!node.fixedNode) //Si a�n no se ha fijado
                    {
                        node.fixedNode = fixer.CheckFixerContainsPoint(node.pos); //Se comprueba si el fixer actual lo contiene, y por tanto, lo fija.
                    }
                }
            }
        }

        triangles = mesh.triangles; //Array que almacena en 3 posiciones consecutivas los �ndices de los v�rtices de cada tri�ngulo.

        for (int i = 0; i < triangles.Length; i += 3) //Recorremos los tri�ngulos.
        {
            //Se crean las 3 aristas del tri�ngulo
            Edge A = new Edge(triangles[i], triangles[i+1], triangles[i+2]);
            Edge B = new Edge(triangles[i], triangles[i + 2], triangles[i + 1]);
            Edge C = new Edge(triangles[i + 1], triangles[i + 2], triangles[i]);

            //Se a�aden al array de aristas.
            edges.Add(A); edges.Add(B); edges.Add(C);
        }

        //Para eliminar aristas duplicadas y crear la estructura DCEL se utiliza el siguiente algoritmo de coste O(N*log(N)):

        edges.Sort(); //Se necesita tener las aristas ordenadas. De esta forma, al recorrer el array, podemos detectar si justo se repiti� una arista.

        Edge previousEdge = null; //Almacenamos una referencia a la arista anterior

        for (int i = 0; i < edges.Count; i++) //Recorremos las aristas.
        {
            if (edges[i].Equals(previousEdge)) //Si la arista actual es igual a la anterior (es una arista repetida)
            {
                springs.Add(new Spring(flexionSpringStiffness, nodes[edges[i].vertexOther], nodes[previousEdge.vertexOther], "flexion")); //Aprovechamos el v�rtice opuesto a la arista para crear el muelle de flexi�n. Se almacena el tipo de muelle en forma de string.
            }
            else //Si no
            {
                springs.Add(new Spring(tractionSpringStiffness, nodes[edges[i].vertexA], nodes[edges[i].vertexB], "traction")); //Agregamos un muelle de tracci�n en la arista. Se almacena el tipo de muelle en forma de string.
            }

            previousEdge = edges[i]; //Actualizamos la referencia a la arista anterior.
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P)) //Pulsar la tecla P activa/desactiva la pausa de la animaci�n.
        {
            paused = !paused;
        }
    }

    //La integraci�n de las f�sicas se realiza en la actualizaci�n de paso fijo, pues as� se evita la acumulaci�n de error.
    private void FixedUpdate()
    {
        h_def = h / substeps; //Actualizamos el paso efectivo en caso de que se modifiquen los par�metros en tiempo de ejecuci�n, aunque la animaci�n este pausada. Se dispone del valor deseado para el paso antes de que se vaya a usar en caso de cualquier cambio de los valores de "h" y "substeps".

        if (paused) //Si la animaci�n est� pausada no hacemos nada.
        {
            return;
        }

        for (int step = 0; step < substeps; step++) //Se realizan uno o varios substeps.
        {
            switch (integrationMethod) //En funci�n del m�todo de integraci�n seleccionado se ejecuta uno u otro.
            {
                case Integration.ExplicitEulerIntegration:
                    IntegrateExplicitEuler();
                    break;
                case Integration.SymplecticEulerIntegration:
                    IntegrateSymplecticEuler();
                    break;
                default:
                    Debug.Log("Error: m�todo de integraci�n no encontrado");
                    break;
            }

            foreach (Spring spring in springs) //Se recorre la lista de muelles tras realizar la integraci�n.
            {
                spring.UpdateSpring(); //Se recalculan los datos del muelle en el siguiente instante.
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = transform.InverseTransformPoint(nodes[i].pos); //Se actualiza la copia del array de v�rtices, pasando de coordenadas globales a locales las nuevas posiciones de los nodos.
            }

            mesh.vertices = vertices; //Se asigna al array de v�rtices del mallado la copia del array de v�rtices modificado.
            mesh.RecalculateBounds(); //Se recalculan los bordes de la malla.
        }
    }

    void IntegrateExplicitEuler() //M�todo que realiza la integraci�n de la velocidad y la posici�n utilizando Euler Expl�cito.
    {
        foreach (Node node in nodes) //Para cada nodo
        {
            if (!node.fixedNode) //Que no sea fijo
            {
                node.pos += h_def * node.vel; //Se integra primero la posici�n, con la velocidad del paso actual.
            }

            node.force = -node.mass * g; //Se aplica la fuerza de la gravedad
            ApplyDampingNode(node); //Se aplica la fuerza de amortiguamiento absoluto al nodo
        }

        foreach (Spring spring in springs) //Para cada muelle, se aplica la fuerza el�stica a los dos nodos que lo componen, en sentidos opuestos por el principio de acci�n y reacci�n.
        {
            spring.nodeA.force += -spring.k * (spring.lenght - spring.lenght0) * spring.dir;
            spring.nodeB.force += spring.k * (spring.lenght - spring.lenght0) * spring.dir;
            ApplyDampingSpring(spring); //Se aplica la fuerza de amortiguamiento de la deformaci�n a cada uno de los nodos que componen el muelle.
        }

        foreach (Node node in nodes) //Para cada nodo
        {
            if (!node.fixedNode) //Que no sea fijo
            {
                node.vel += h_def * node.force / node.mass; //Se integra la velocidad con la fuerza actual calculada.
            }
        }
    }

    void IntegrateSymplecticEuler() //M�todo que realiza la integraci�n de la velocidad y la posici�n utilizando Euler Simpl�ctico.
    {
        foreach (Node node in nodes) //Para cada nodo
        {
            node.force = -node.mass * g; //Se aplica la fuerza de la gravedad.
            ApplyDampingNode(node); //Se aplica la fuerza de amortiguamiento absoluto al nodo
        }

        foreach (Spring spring in springs) //Para cada muelle, se aplica la fuerza el�stica a los dos nodos que lo componen, en sentidos opuestos por el principio de acci�n y reacci�n.
        {
            spring.nodeA.force += -spring.k * (spring.lenght - spring.lenght0) * spring.dir;
            spring.nodeB.force += spring.k * (spring.lenght - spring.lenght0) * spring.dir;
            ApplyDampingSpring(spring); //Se aplica la fuerza de amortiguamiento de la deformaci�n a cada uno de los nodos que componen el muelle.
        }

        foreach (Node node in nodes) //Para cada nodo
        {
            if (!node.fixedNode) //Que no sea fijo
            {
                node.vel += h_def * node.force / node.mass; //Se integra la velocidad con la fuerza actual calculada.
                node.pos += h_def * node.vel; //Se utiliza la velocidad en el paso siguiente para integrar la nueva posici�n.
            }
        }
    }

    void ApplyDampingNode(Node node) //M�todo que aplica el amortiguamiento absoluto del nodo.
    {
        node.force += -dAbsolute * node.vel; //Se logra aplicando una fuerza proporcional a la velocidad ajustada por el coeficiente de amortiguamiento absoluto, en sentido contrario de la velocidad. Simula el rozamiento con el aire.
    }

    void ApplyDampingSpring(Spring spring) //M�todo que aplica el amortiguamiento de la deformaci�n del muelle.
    {
        //A cada nodo del muelle se le aplica la misma fuerza en sentido contrario por el principio de acci�n y reacci�n.
        //La fuerza de amortiguamiento de la deformaci�n depende de las velocidades relativas de los nodos del muelle y la direcci�n del muelle, ajustada por un coeficiente de amortiguamiento de la deformaci�n.
        spring.nodeA.force += -dDeformation * Vector3.Dot(spring.dir, (spring.nodeA.vel - spring.nodeB.vel)) * spring.dir;
        spring.nodeB.force += dDeformation * Vector3.Dot(spring.dir, (spring.nodeA.vel - spring.nodeB.vel)) * spring.dir;
    }

    private void OnDrawGizmos() //Funci�n de evento de Unity que se ejecuta en cada vuelta del bucle del juego para redibujar los Gizmos.
    {
        foreach (Spring spring in springs) //Se recorre la lista de muelles, y en funci�n del tipo del muelle se utiliza un color u otro.
        {
            if(spring.springType == "flexion")
            {
                Gizmos.color = Color.blue; //Muelles de flexi�n de color azul.
            }
            else
            {
                Gizmos.color = Color.red; //Muelles de tracci�n de color rojo.
            }

            Gizmos.DrawLine(spring.nodeA.pos, spring.nodeB.pos); //Se dibuja una l�nea entre el par de nodos del muelle.
        }

        Gizmos.color = Color.black; //Se cambia a color negro.

        foreach (Node node in nodes) //Se recorren los nodos.
        {
            Gizmos.DrawSphere(node.pos, 0.05f); //Se pinta una esfera en cada uno de los nodos.
        }
    } //Estos Gizmos nos permiten ver en tiempo real el movimiento de los v�rtices y los distintos tipos de muelles.
}
