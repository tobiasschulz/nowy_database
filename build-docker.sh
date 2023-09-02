#!/bin/bash

cd "$(dirname "$(readlink -f "${BASH_SOURCE[0]}" )" )"

set -euxo pipefail

function retry () {
    "$@" || "$@" || "$@" || "$@" || "$@"
}

docker image build -f src/Nowy.Database.Web/Dockerfile .   -t tobiasschulzdev/nowy-database-web:master   -t tobiasschulzdev/lr-database-web:master    -t tobiasschulzdev/ts-database-web:master
docker image build -f src/nowy_messagehub/Dockerfile .     -t tobiasschulzdev/nowy-messagehub-web:master -t tobiasschulzdev/lr-messagehub-web:master  -t tobiasschulzdev/ts-messagehub-web:master

for tenant in ts
do
    for microservice in database-web messagehub-web
    do
        retry docker push tobiasschulzdev/${tenant}-${microservice}:master
    done
done

ssh -o StrictHostKeyChecking=no root@wellspring.leuchtraketen.cloud 'kubectl rollout restart deploy,statefulset -n nowy-database'
ssh -o StrictHostKeyChecking=no root@wellspring.leuchtraketen.cloud 'kubectl rollout restart deploy,statefulset -n nowy-messagehub'

