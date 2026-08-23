namespace Tests.E2eTests
{
    /// <summary>
    /// E2E 테스트가 조작할 수 있는 게임 오브젝트의 식별자.
    /// </summary>
    /// <remarks>
    /// 등록 방법은 <see cref="TestId"/>, 조회 방법은 <see cref="GameObjectRegistryForTest"/> 참고.
    /// </remarks>
    public enum GameObjectId
    {
        SinglePlayButton,
        MultiPlayButton,
        AutoMatchingButton,
        PrivateRoomCreateButton,
        PrivateRoomParticipateButton,
        InvitationCodeInputField,
        InvitationCodeReadField,
        Tilemap,
    }
}
