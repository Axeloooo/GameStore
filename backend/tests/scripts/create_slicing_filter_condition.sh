#!/bin/bash

# Property name used for test filtering
filterProperty="FullyQualifiedName"

# Capture all script arguments as test names
tests=("$@")
testCount=${#tests[@]}

# Get Azure DevOps parallel job info (total agents and this agent's position)
totalAgents=$SYSTEM_TOTALJOBSINPHASE
agentNumber=$SYSTEM_JOBPOSITIONINPHASE

# Safety: prevent division by zero and handle missing agent number
if [ $totalAgents -eq 0 ]; then totalAgents=1; fi
if [ -z "$agentNumber" ]; then agentNumber=1; fi

echo "Total agents: $totalAgents"
echo "Agent number: $agentNumber"
echo "Total tests: $testCount"

echo "Target tests:"
# Distribute tests across agents using round-robin: agent 1 gets tests 1,4,7..., agent 2 gets 2,5,8..., etc.
for ((i=$agentNumber; i <= $testCount;i=$((i+$totalAgents)))); do
    targetTestName=${tests[$i -1]}  # Array is 0-indexed, agent numbers are 1-indexed
    echo "$targetTestName"

    # Build space-delimited filter string of test names for this agent
    if [ -z "$filter" ]; then
        filter="$targetTestName"
    else
        filter="$filter $targetTestName"
    fi
done

# Set Azure DevOps pipeline variable for downstream tasks to consume
echo "##vso[task.setvariable variable=targetTestsFilter]$filter"