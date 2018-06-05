#!/bin/bash
if (( $# < 6 ));then echo "Need at least 6 Parameters"; exit 0;fi
if [ ! -d $1 ];then mkdir -p $1;fi
logFilePath="$1/$(date '+%Y-%m-%d_%H·%M·%S')_ModifyMeta.txt"
rootPath=$2
platform=$3
startIndex=4

#OLD_IFS=$IFS
IFS=$'\n'
files=(`find ${rootPath} -name '*.meta' -type f`)
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

    file_content=$(cat ${file} 2>&1)
    file_content_origin=$file_content
    returnValue=$?
    if [ $returnValue != 0 ];then
        #错误信息
        errorMessage="Error(${returnValue}): ${file_content} \t FilePath: ${file} \t Operation: cat"
        echo -e "Error: ${errorMessage}"
        echo -e "${errorMessage}" >> ${logFilePath}
        #统计信息
        echo "Skip: ${skipCount}"
        echo "Success: ${successCount}"
        doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
        echo -e "${doneMessage}" >> ${logFilePath}

        exit 1
    fi

    for((i=${startIndex};i<=$#;))
    do
        eval b=\${$((i++))}
        option=$b
        eval b=\${$((i++))}
        origin=$b
        eval b=\${$((i++))}
        target=$b

        operation="/buildTarget: ${platform}/,/buildTarget/s/${option}: ${origin}.*$/${option}: ${target}/"

        file_content=$( echo "${file_content}" | sed -e ${operation} 2>&1 )
        returnValue=$?
        if [ $returnValue != 0 ];then
            #错误信息
            errorMessage="Error(${returnValue}): ${file_content} \t FilePath: ${file} \t Operation: sed -e ${operation}"
            echo -e "Error: ${errorMessage}"
            echo -e "${errorMessage}" >> ${logFilePath}
            #统计信息
            echo "Skip: ${skipCount}"
            echo "Success: ${successCount}"
            doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
            echo -e "${doneMessage}" >> ${logFilePath}

            exit 2
        fi
    done
    if [ "${file_content}" == "${file_content_origin}" ];then
        ((skipCount++))
        echo -e "Skip: ${file} \t Reason: No Change." >> ${logFilePath}
    else
        echo "${file_content}" > "${file}"
        returnValue=$?
        if [ $returnValue != 0 ];then
            #错误信息
            errorMessage="Error(${returnValue}): Can Not Write File. \t FilePath: ${file} \t Operation: Write File."
            echo -e "Error: ${errorMessage}"
            echo -e "${errorMessage}" >> ${logFilePath}
            #统计信息
            echo "Skip: ${skipCount}"
            echo "Success: ${successCount}"
            doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
            echo -e "${doneMessage}" >> ${logFilePath}

            exit 3
        fi
        echo -e "Success: ${file}" >> ${logFilePath}
        ((successCount++))
    fi
done
#统计信息
echo "Skip: ${skipCount}"
echo "Success: ${successCount}"
doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
echo -e "${doneMessage}" >> ${logFilePath}
