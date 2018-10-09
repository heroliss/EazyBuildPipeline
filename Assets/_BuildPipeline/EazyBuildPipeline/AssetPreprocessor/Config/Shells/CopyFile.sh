#!/bin/bash
function Log()
{
    echo -e $* | awk -F ' \t ' '{printf ("%-10s \t %-21s \t %-200s \t %s\n", $1, $2, $3, $4)}' >> ${logFilePath} #此处用“ \t ”做分隔符，每列宽度依次应为4的倍数-2,-3,-0,-1...
}

if (( $# < 4 ));then echo "Need at least 4 Parameters"; exit 0;fi
if [ ! -d $1 ];then mkdir -p $1;fi
logFilePath="$1/$(date '+%Y-%m-%d_%H.%M.%S')_CopyFile.txt"
targetRoot=$2
originRoot=$3
matchTags="find ${originRoot} -type f -name '*\[$4\]*'"
startIndex=5
for((i=${startIndex};i<=$#;i++))
do
    eval b=\$$i
    matchTags="${matchTags} -o -name '*\[${b}\]*'"
done

#OLD_IFS=$IFS
IFS=$'\n'
files=(`eval ${matchTags}`)
#IFS=$OLD_IFS
filesNum=${#files[@]}
count=0
successCount=0
skipCount=0

#开始信息
echo "LogFilePath: ${logFilePath}"
echo "Total: ${filesNum}"
Log "Shell: $0"
Log "Parameters: $*"
Log "Start at $(date '+%Y-%m-%d %H:%M:%S')\n"
Log "State \t Message \t Target File Path \t Source File Path"
Log "---------- \t --------------------- \t -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- \t --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------"

for file in ${files[@]}
do
    echo "Applying: $((count++)) ${file}"
    targetFile=${file/#${originRoot}/${targetRoot}}
    targetFile=$(echo ${targetFile} | sed "s/\[[^][]*\]//g")
    if [ -f ${targetFile} ];then
        errorStr=$( cp -f ${file} ${targetFile} 2>&1 )
        returnValue=$?
        if [ $returnValue != 0 ];then
            #错误信息
            errorMessage="Error(${returnValue}) \t ${errorStr} (Operation: cp -f) \t ${targetFile} \t ${file}"
            echo -e "Error: ${errorMessage}"
            Log "${errorMessage}"
            #统计信息
            echo "Skip: ${skipCount}"
            echo "Success: ${successCount}"
            doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
            Log "${doneMessage}"

            exit 1
        fi
        ((successCount++))
        Log "Success \t  \t ${targetFile} \t ${file}"
    else
        ((skipCount++))
        Log "Skip \t TargetFile Not Exist. \t ${targetFile} \t ${file}"
    fi
done
#统计信息
echo "Skip: ${skipCount}"
echo "Success: ${successCount}"
doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
Log "${doneMessage}"