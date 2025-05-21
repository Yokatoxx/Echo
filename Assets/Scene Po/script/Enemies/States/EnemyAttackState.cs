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
        base.EnterState();

        if (numberOfDeaths < deathsBeforeReset)
        {
            player.transform.position = spawnPoint.transform.position;
            numberOfDeaths = numberOfDeaths + 1;

            Debug.Log("Player is dead " + numberOfDeaths);

            enemy.stateMachine.ChangeState(enemy.iddleState);

        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Debug.Log("Game Over");
        }
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (numberOfDeaths < enemy.NumberOfDeathsBeforeReset)
        {
            player.transform.position = spawnPoint.transform.position;
            numberOfDeaths = numberOfDeaths + 1;


            Debug.Log("Player is dead " + numberOfDeaths);

            enemy.stateMachine.ChangeState(enemy.iddleState);

        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Debug.Log("Game Over");
        }

    }



    public override void ExitState()
    {
        base.ExitState();

        enemy.SetAttackDistanceBool(false);

    }

}
