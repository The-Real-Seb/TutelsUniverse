using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalNavMesh : MonoBehaviour
{
    [Header("Data")]
    public Animal Animal_Data;
    public TimeParam Time_Data;

    [Header("Personal Variables")]
    public float actualPV;
    public float actualDmg;
    public float foodGauge = 100;
    public float age = 1f;
    public float anneeSpawn = 0;
    public bool isLeader = false;
    public GameObject cible;
    public Statut statut;

    [Header("Contact Lists")]
    public List<GameObject> vueList;
    public List<GameObject> contactList;
    public List<GameObject> ennemisList;
    

    [Header("Object Reference")]
    public MeshFilter mesh;
    public GameObject vue;
    public GameObject contact;

    public Animator animatorAnimal;
    private NavMeshAgent agent;
    public float timeLeft;
    private float timeFoodLeft;

    private float sizeMultiply;

    public enum Statut
    {
        Agressif,
        Passif,
    };



    private void Awake()
    {
        InitAnimal();
        agent.updateRotation = false;
        
    }
    private void LateUpdate()
    {
        if (agent.velocity.sqrMagnitude > Mathf.Epsilon)
        {
            transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
        }
    }

    private void Update()
    {
        RefreshPv();
        RefreshAge();
        RefreshSize();
        RefreshFood();

        actualDmg = Mathf.Clamp(actualDmg, 0, Animal_Data._PVMax * Animal_Data._CourbeVitalite.Evaluate(age / Animal_Data._Longevite));
    }

    public void CheckLeader()
    {
        if (vueList.Count > 0 && !isLeader)
        {
            foreach (GameObject obj in vueList)
            {
                if (obj.GetComponent<AnimalNavMesh>().isLeader)
                {
                    isLeader = false;
                }
                else
                {
                    isLeader = true;
                }
            }
        }
        else if(!isLeader)
        {
            isLeader = true;
        }
    }        

    public void InitAnimal()
    {
        RefreshPv();
        mesh.mesh = Animal_Data._Mesh;
        agent = GetComponent<NavMeshAgent>();
        animatorAnimal = GetComponent<Animator>();
        agent.speed = Animal_Data._VitesseMax;
        timeLeft = Random.Range(0f, 2f);
        mesh.gameObject.GetComponent<MeshRenderer>().material = Animal_Data._Material;
        anneeSpawn = Time_Data._Annee;
        //CheckLeader();
    }
   

    void RefreshFood()
    {
        timeFoodLeft -= Time.deltaTime * Time_Data._SelectedSpeed;

        if (timeFoodLeft < 0 && Time_Data._SelectedSpeed != 0)
        {
            timeFoodLeft = Time_Data._RateAnnee;
            foodGauge--;
            animatorAnimal.SetFloat("Food", foodGauge);
            if (foodGauge < 0)
            {
                Destroy(transform.gameObject);
            }
        }
    }

    void RefreshPv()
    {
        if(age < Animal_Data._Longevite)
        {            
            actualPV = Animal_Data._PVMax * Animal_Data._CourbeVitalite.Evaluate(age / Animal_Data._Longevite) - actualDmg;
            animatorAnimal.SetFloat("PV", actualPV);
        }
        else
        {
            Destroy(transform.gameObject);
        }

        if(actualPV < 0)
        {
            Destroy(transform.gameObject);
        }

    }

    void RefreshSize()
    {
        setLeaderSize();
        Vector3 scale = (Vector3.one * Animal_Data._CourbeScale.Evaluate(age / Animal_Data._Longevite)); 
        scale += Vector3.one * sizeMultiply;
        mesh.transform.localScale = Vector3.Lerp(mesh.transform.localScale, scale, Time.deltaTime);
    }

    
    private void setLeaderSize()
    {
        if (isLeader)
        {
            sizeMultiply = 2f;
        }
        else
        {
            sizeMultiply = 1f;
        }
    }

    public void RefreshAge()
    {
        age = Time_Data._Annee - anneeSpawn;
        animatorAnimal.SetFloat("Age", age);
    }
    
    public void Movement()
    {
        agent.speed = Animal_Data._VitesseMax * Time_Data._SelectedSpeed;
        Vector3 destination = transform.position;
        

        if (vueList.Count > 0 && !isLeader)
        {
            
            foreach (GameObject obj in vueList)
            {
                if(obj.GetComponent<AnimalNavMesh>().isLeader)
                    destination = obj.transform.position;
            }            
        }
        else
        {
            destination = transform.position;
        }

        destination += (Random.insideUnitSphere * 15);
        destination.y = transform.position.y;


        timeLeft -= Time.deltaTime * Time_Data._SelectedSpeed;

        if (timeLeft < 0 && Time_Data._SelectedSpeed != 0)
        {
            timeLeft = Time_Data._RateAnnee - Random.Range(-2f, Time_Data._RateAnnee);
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
            }
            else
            {
                Destroy(transform);
            }
                
        }        
    }
    

    public void Chasse()
    {       
        if (ennemisList.Count > 0)
        {
            Vector3 destination = transform.position;
            destination = ennemisList[0].transform.position;
            cible = ennemisList[0];
            destination.y = transform.position.y;

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
            }
        }
        else
        {
            cible = null;
            //statut = Statut.Passif;
        }
    }

    private float attTime;
    public void Attack()
    {     
        if (cible != null && contactList.Contains(cible))
        {
            attTime -= Time.deltaTime * Time_Data._SelectedSpeed;
            if (attTime < 0 && Time_Data._SelectedSpeed != 0)
            {
                attTime = 0.5f;
                cible.GetComponent<AnimalNavMesh>().actualDmg += Animal_Data._Degats;
            }                
        }
        else
        {
            animatorAnimal.SetBool("Fight", false);
        }
    }

    public void Fuite()
    {
        agent.speed = Animal_Data._VitesseMax * Time_Data._SelectedSpeed * 1.5f;
        Vector3 destination = new Vector3(0,0,0);
        if (ennemisList.Count > 0)
        {
            foreach (GameObject obj in ennemisList)
            {                
                destination += obj.transform.position;
            }

            destination /= ennemisList.Count;
            
            Vector3 newdestination = transform.position + ((transform.position - destination).normalized) * 5;            
            newdestination.y = transform.position.y;
        

            agent.SetDestination(newdestination);
            Debug.Log(newdestination);
        }
        
    }

    public bool CheckRace(GameObject animal)
    {
        if(animal.GetComponent<AnimalNavMesh>().Animal_Data._Regime == Animal.Regime.Carnivore)
        {
            ennemisList.Add(animal);
            return true;
        }
        return false;
    }

    
}
