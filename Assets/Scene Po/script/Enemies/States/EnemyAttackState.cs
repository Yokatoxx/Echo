using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class EnemyAttackState : EnemyState
{
    private GameObject player;
    private GameObject spawnPoint;
    private int numberOfDeaths = 0;
    private int deathsBeforeReset;

    public EnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        spawnPoint = GameObject.FindGameObjectWithTag("SpawnPoint");

        player = GameObject.FindGameObjectWithTag("Player");
        numberOfDeaths = 0;

        deathsBeforeReset = enemy.NumberOfDeathsBeforeReset;

    }

    public override void EnterState()
    {
        Debug.Log("attack Player");


        if (numberOfDeaths >= deathsBeforeReset)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (numberOfDeaths < deathsBeforeReset)
        {
            Debug.Log("no Game Over");

            player.transform.position = spawnPoint.transform.position;
            Physics.SyncTransforms();

            numberOfDeaths = numberOfDeaths + 1;
            enemy.stateMachine.ChangeState(enemy.iddleState);

        }
    }
    

    public override void ExitState()
    {
        base.ExitState();

        enemy.SetAttackDistanceBool(false);

    }

}
