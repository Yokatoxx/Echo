using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ItriggerCheckable 
{
    bool IsAggroed { get; set; }

    bool IsWithinAttackDistance { get; set; }

    bool IsWithinPickUpDistance { get; set; }

    void SetAggroStatus(bool isAggroed);
    void SetAttackDistanceBool(bool isWithinAttackDistance);

    void SetPickUpDistanceBool(bool isWithinPickUpDistance);
}
