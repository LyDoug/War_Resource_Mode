# Resource War — Contexto para Assistente de IA

## Objetivo do projeto
Resource War é um jogo de estratégia e combate com veículos militares, desenvolvido em Unity por um desenvolvedor solo.

A mecânica central envolve capturar estruturas estratégicas, gerar recursos, produzir veículos e reduzir os tickets do adversário até zero.

## Sistemas existentes
- GameManager: tickets, equipes, estruturas, recursos, vitória e modos de emergência.
- VehicleSpawnerFinal: criação de veículos usando VehicleBlueprint.
- VehicleBlueprint: dados dos veículos, custos, papéis e requisitos de desbloqueio.
- VehicleController: ativação e desativação do controle dos veículos.
- VehicleMotor e TankMotor: física e movimentação.
- VehicleMotorBase: classe abstrata compartilhada pelos motores.
- DeployableVehicle: alternância entre modo transporte e modo estacionário.
- VehicleZoneState e ZoneTrigger: detecção das zonas.
- ArtilleryPosition e ArtilleryPositionManager: posições fixas de artilharia.
- RadarStation: radar de cada equipe, que pode ser destruído ou reparado.
- ArtilleryComputer e ArtilleryCamera: computador de artilharia e câmera para tiro indireto.
- TeamMember: identificação da equipe.
- BaseInventory e TruckCargo: armazenamento e transporte de recursos.

## Mecânica da SPG
A artilharia autopropulsada possui dois modos:

**Deployed**
- Veículo imóvel.
- Arma principal habilitada.
- Na base principal, pode utilizar o computador de artilharia quando o radar da equipe estiver operacional.

**Transport**
- Veículo pode se movimentar.
- Arma principal desabilitada.
- Pode viajar até as zonas de Deployment.

Controles planejados:
- T: montar a artilharia em uma zona permitida.
- G: desmontar e entrar em transporte.

Ao nascer em ArtilleryPosition, a SPG deve começar automaticamente montada.

## Zonas
- Base: posição inicial de artilharia e acesso ao radar.
- Combat: deslocamento em modo transporte.
- Deployment: permite montar a artilharia para disparos manuais.

## Radar
Cada equipe possui seu próprio RadarStation.

O computador de artilharia deve consultar o radar da equipe do veículo, sem depender de um RadarSystem separado.

Se o radar for destruído, o computador deve ser desativado.

## Refatoração em andamento
Estamos unificando VehicleMotor e TankMotor através de VehicleMotorBase.

A classe base possui:
- EnableMovement(bool)
- CanMove()
- movementEnabled
- Rigidbody compartilhado

O DeployableVehicle ainda precisa ser consolidado para utilizar VehicleMotorBase, sem depender especificamente de VehicleMotor.

## Regras para alterações
1. Ler os scripts existentes antes de modificá-los.
2. Preservar sistemas de spawn, blueprints, captura e troca de veículos.
3. Não inventar métodos, campos ou enums sem verificar se existem.
4. Evitar alterar múltiplos sistemas simultaneamente.
5. Corrigir primeiro erros de compilação e depois problemas de comportamento.
6. Sempre explicar quais arquivos foram modificados.
7. Priorizar soluções simples, modulares e de baixo custo.

## Próxima tarefa
Revisar VehicleMotorBase, VehicleMotor, TankMotor e DeployableVehicle para concluir a refatoração da movimentação.

Depois continuar o desenvolvimento do computador de artilharia integrado ao RadarStation.
