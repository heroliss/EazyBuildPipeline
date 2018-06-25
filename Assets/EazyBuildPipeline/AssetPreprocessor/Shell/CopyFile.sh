#!/bin/bash
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
echo -e Shell: $0 >> ${logFilePath}
echo -e Parameters: $* >> ${logFilePath}
echo -e "Start at $(date '+%Y-%m-%d %H:%M:%S')\n" >> ${logFilePath}

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
            errorMessage="Error(${returnValue}): ${errorStr} \t FilePath: ${file} \t Operation: cp -f ${file} ${targetFile}"
            echo -e "Error: ${errorMessage}"
            echo -e "${errorMessage}" >> ${logFilePath}
            #统计信息
            echo "Skip: ${skipCount}"
            echo "Success: ${successCount}"
            doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
            echo -e "${doneMessage}" >> ${logFilePath}

            exit 1
        fi
        ((successCount++))
        echo -e "Success: ${file} \t Copyto: ${targetFile}" >> ${logFilePath}
    else
        ((skipCount++))
        echo -e "Skip: ${file} \t Reason: TargetFile Not Exist. (${targetFile})" >> ${logFilePath}
    fi
done
#统计信息
echo "Skip: ${skipCount}"
echo "Success: ${successCount}"
doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
echo -e "${doneMessage}" >> ${logFilePath}
