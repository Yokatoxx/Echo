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

    public EnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        spawnPoint = GameObject.FindGameObjectWithTag("SpawnPoint");

        player = GameObject.FindGameObjectWithTag("Player");
        numberOfDeaths = 0;

    }

    public override void EnterState()
    {
        base.EnterState();

        enemy.SetAttackDistanceBool(false);

        if (numberOfDeaths < enemy.NumberOfDeathsBeforeReset)
        {
            respawnPlayer();
        }
        else
        {
            restart();
        }

    }

    public override void ExitState()
    {
        base.ExitState();
        Debug.Log(numberOfDeaths + "Deaths");
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();


    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public void respawnPlayer()
    {
        player.transform.position = spawnPoint.transform.position;
        numberOfDeaths++;
        Debug.Log("Player respawned");
        enemy.stateMachine.ChangeState(enemy.iddleState);
    }

    public void restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Game Over");

    }

}
